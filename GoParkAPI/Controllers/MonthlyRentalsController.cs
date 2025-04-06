using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoParkAPI.Models;
using Azure.Core;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using GoParkAPI.DTO;
using GoParkAPI.Services;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonthlyRentalsController : ControllerBase
    {
        private readonly EasyParkContext _context;
        private readonly MonRentalService _monRentalService;

        public MonthlyRentalsController(EasyParkContext context, MonRentalService monRentalService)
        {
            _context = context;
            _monRentalService = monRentalService;
        }

        //先抓出該用戶註冊的車牌id
        private async Task<List<int>> GetUserCars(int userId)
        {
            return await _context.Car
                .Where(car => car.UserId == userId)
                .Select(car => car.CarId)
                .ToListAsync();
        }

        // GET: api/MonthlyRentals
        [HttpGet]
        public async Task<IEnumerable<MonthlyRentalDTO>> GetMonthlyRentals(int userId, string? licensePlate)
        {
            //根據 userId抓出用戶的車牌id
            var userCars = await GetUserCars(userId);

            //篩選該用戶車牌的預訂資料
            var rentals = _context.MonthlyRental
                .Where(rental => userCars.Contains(rental.CarId)) // 比對車牌號碼
                .Where(rental => string.IsNullOrEmpty(licensePlate) || rental.Car.LicensePlate == licensePlate) //若有填寫車牌則進一步篩選
                .Select(rental => new MonthlyRentalDTO
                {
                    renId = rental.RenId,
                    licensePlate = rental.Car.LicensePlate,
                    lotId = rental.LotId,
                    lotName = rental.Lot.LotName,
                    latitude = rental.Lot.Latitude,
                    longitude = rental.Lot.Longitude,
                    location = rental.Lot.Location,
                    district = rental.Lot.District,
                    startDate = (DateTime)rental.StartDate,
                    endDate = (DateTime)rental.EndDate,
                    amount = rental.Amount,  //付的總額
                    monRentalRate = rental.Lot.MonRentalRate
                })
                .OrderBy(rental => rental.endDate); // 最早到期的在前面;

            if (rentals == null)
            {
                return null;
            }
            return rentals;


        }

        // GET: api/MonthlyRentals/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<MonthlyRental>> GetMonthlyRental(int id)
        //{
        //    var monthlyRental = await _context.MonthlyRentals.FindAsync(id);

        //    if (monthlyRental == null)
        //    {
        //        return NotFound();
        //    }

        //    return monthlyRental;
        //}

        // PUT: api/MonthlyRentals/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMonthlyRental(int id, MonthlyRental monthlyRental)
        {
            if (id != monthlyRental.RenId)
            {
                return BadRequest();
            }

            _context.Entry(monthlyRental).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MonthlyRentalExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/MonthlyRentals
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<MonthlyRental>> PostMonthlyRental(MonthlyRental monthlyRental)
        {
            _context.MonthlyRental.Add(monthlyRental);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMonthlyRental", new { id = monthlyRental.RenId }, monthlyRental);
        }

        // DELETE: api/MonthlyRentals/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMonthlyRental(int id)
        {
            var monthlyRental = await _context.MonthlyRental.FindAsync(id);
            if (monthlyRental == null)
            {
                return NotFound();
            }

            _context.MonthlyRental.Remove(monthlyRental);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("CheckMonRentalSpace")]
        public async Task<IActionResult> CheckMonRentalSpace(int lotId, int userId)
        {
            var user = await _context.Customer.FirstOrDefaultAsync(u => u.UserId == userId);
            if(user != null && user.IsBlack == true) //黑名單用戶
            {
                return Ok(new { Message = "黑名單用戶無法申請月租", Success = false });
            }

            bool isAvailable = await _monRentalService.isMonResntalSpaceAvailableAsync(lotId);
            if (!isAvailable)
            {
                var parkinglots = await _monRentalService.GetParkingLotAsync(lotId);
                if (parkinglots == null)
                {
                    return Ok(new { Message = "無效的停車場ID", Success = false });
                }
                if (parkinglots.MonRentalRate <= 0)
                {
                    return Ok(new { Message = "該停車場不支援月租服務", Success = false });
                }
                return Ok(new { Message = "月租車位已滿, 您可以填寫申請表單等待抽籤", Success = false });
            }
            return Ok(new { Message = "月租車位可用", Success = true });
        }

        [HttpPost("newMonApplyList")]
        public async Task<IActionResult> newMonApplyList([FromBody] MonApplyDTO monApplayDTO)
        {
            try
            {
                var user = await _context.Customer.FirstOrDefaultAsync(u => u.UserId == monApplayDTO.UserId);
                if(user != null && user.IsBlack == true) //黑名單用戶
                {
                    return BadRequest(new { Message = "黑名單用戶無法申請月租" });
                }

                if (monApplayDTO.UserId == null || monApplayDTO.UserId == 0)
                {
                    return BadRequest(new { Message = "無法取得用戶ID" });
                }

                var car = await _context.Car.FirstOrDefaultAsync(c => c.LicensePlate == monApplayDTO.LicensePlate && c.UserId == monApplayDTO.UserId);
                if (car == null)
                {
                    return BadRequest(new { Message = "無法找到對應的車輛" });
                }

                var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                var taiwanTime = TimeZoneInfo.ConvertTime(DateTime.Now, taiwanTimeZone);

                //寫進MonApplyList
                var newApplay = new MonApplyList
                {
                    CarId = car.CarId,
                    LotId = monApplayDTO.LotId,
                    ApplyDate = taiwanTime,
                };
                _context.MonApplyList.Add(newApplay);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "申請已成功提交", applyID = newApplay.ApplyId });//debug方便給前端一個id
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        private bool MonthlyRentalExists(int id)
        {
            return _context.MonthlyRental.Any(e => e.RenId == id);
        }
    }
}
