using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoParkAPI.Models;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using GoParkAPI.DTO;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Cars_Controller : ControllerBase
    {
        private readonly EasyParkContext _context;

        public Cars_Controller(EasyParkContext context)
        {
            _context = context;
        }

        //取得用戶車牌資料
        // GET: api/Cars_
        [HttpGet]
        public async Task<IEnumerable<CarsDTO>> GetCars(int userId)
        {
            var cars =  _context.Car
                .Where(car => car.UserId == userId)
                .Select(car => new CarsDTO
                {
                    carId = car.CarId,
                    licensePlate = car.LicensePlate,
                    registerDate = car.RegisterDate,
                    isActive = car.IsActive
                });
            if (cars == null)
            {
                return null;
            }
            return cars;
        }

        //修改車牌使用狀態
        // PUT: api/Cars_/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut()]
        public async Task<string> PutCars(List<CarsDTO> carsDto)
        {
            foreach (var car in carsDto)
            {
                // 找到對應的車輛
                var updateCar = await _context.Car.FindAsync(car.carId);
                if (updateCar == null)
                {
                    return "修改失敗";
                }
                updateCar.IsActive = car.isActive;
                _context.Entry(updateCar).Property(c => c.IsActive).IsModified = true;
                
            }
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return "修改成功";
        }

        // POST: api/Cars_
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostCar(int userId, List<CarsDTO> carsDto)
        {
            
            foreach (var car in carsDto)
            {
                //驗證車牌是否為空白                
                if (string.IsNullOrWhiteSpace(car.licensePlate))
                {
                    return BadRequest(new { success = false, message = "新增失敗: 車牌不能為空" });
                }

                // 驗證車牌是否已存在
                bool carExists = await _context.Car.AnyAsync(c => c.LicensePlate == car.licensePlate);
                if (carExists)
                {
                    return BadRequest(new { success = false, message = $"新增失敗: 車牌 {car.licensePlate} 已存在" });
                }

                var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                var taiwanTime = TimeZoneInfo.ConvertTime(DateTime.Now, taiwanTimeZone);

                Car newCar = new Car
                {
                    UserId = userId,
                    LicensePlate = car.licensePlate,
                    RegisterDate = taiwanTime,
                    IsActive = car.isActive
                };
                _context.Car.Add(newCar);
            };
            

            try
            {
                // 儲存所有新增的車牌資料
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { success = false, message = $"新增失敗: {ex.Message}" });
            }

            return Ok(new { success = true, message = "新增成功" });
        }


        private bool CarExists(int id)
        {
            return _context.Car.Any(e => e.CarId == id);
        }
    }
}
