using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoParkAPI.Models;
using GoParkAPI.DTO;
using GoParkAPI.Services;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly EasyParkContext _context;

        public ReservationsController(EasyParkContext context)
        {
            _context = context;
        }

        //先抓出該用戶註冊的車牌
        private async Task<List<string>> GetUserCars(int userId)
        {
            return await _context.Car
                .Where(car => car.UserId == userId)
                .Select(car => car.LicensePlate)
                .ToListAsync();
        }

        //載入用戶的預訂資料 & 透過車牌篩選(Option)
        // GET: api/Reservations
        [HttpGet]
        public async Task<IEnumerable<ReservationDTO>> GetReservation(int userId, string? licensePlate)
        {
            //根據 userId抓出用戶的車牌號碼
            var userCars = await GetUserCars(userId);

            //篩選該用戶車牌的預訂資料
            var reservations = _context.Reservation
                .Where(res => userCars.Contains(res.Car.LicensePlate)) // 比對車牌號碼
                .Where(res => string.IsNullOrEmpty(licensePlate) || res.Car.LicensePlate == licensePlate) //若有填寫車牌則進一步篩選
                .Select(res => new ReservationDTO
                {
                    resId = res.ResId,
                    resTime = (DateTime)res.ResTime,
                    lotName = res.Lot.LotName,
                    district = res.Lot.District,
                    location = res.Lot.Location,                    
                    licensePlate = res.Car.LicensePlate,
                    startTime = (DateTime)res.StartTime,
                    validUntil = (DateTime)res.ValidUntil,  //還在進行中的預定要顯示
                    PaymentStatus =res.PaymentStatus,
                    isCanceled = res.IsCanceled,
                    isOverdue = res.IsOverdue, //判斷是否逾時(違規)
                    isFinish = res.IsFinish,
                    latitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Latitude).FirstOrDefault(),
                    longitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Longitude).FirstOrDefault(),
                    lotId = res.Lot.LotId, //為了要導入到預定頁面需要停車場id
                    
                });
            if (reservations == null)
            {
                return null;
            }
            return reservations;
        }

        //搜尋"停車場"載入預定紀錄
        [HttpGet("search")]
        public async Task<IEnumerable<ReservationDTO>> SearchReservationsByLotname(int userId, string district)
        {
            //根據 userId抓出用戶的車牌號碼
            var userCars = await GetUserCars(userId);
           

            // 根據停車場名稱模糊查詢訂單
            var reservations = _context.Reservation
                .Where(res => userCars.Contains(res.Car.LicensePlate) && res.Lot.District.Contains(district))
                .Select(res => new ReservationDTO
                {
                    resId = res.ResId,
                    licensePlate = res.Car.LicensePlate,
                    resTime = (DateTime)res.ResTime,
                    lotName = res.Lot.LotName,
                    district = res.Lot.District,
                    location = res.Lot.Location,
                    isCanceled = res.IsCanceled,
                    isOverdue = res.IsOverdue,
                    isFinish = res.IsFinish,
                    latitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Latitude).FirstOrDefault(),
                    longitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Longitude).FirstOrDefault()

                });

            if (reservations == null)
            {
                return null;
            }
            return reservations;
        }

        //篩選預訂狀態(完成、取消、逾時)
        [HttpGet("filter")]
        public async Task<IEnumerable<ReservationDTO>> reservationFilter(int userId, string filter)
        {
            //根據 userId抓出用戶的車牌號碼
            var userCars = await GetUserCars(userId);
            IQueryable<Reservation> res = null;

            switch (filter)
            {
                case "isCompleted":
                    // 返回已使用的優惠券
                    res = _context.Reservation
                        .Where(res => userCars.Contains(res.Car.LicensePlate) && res.IsFinish && !res.IsOverdue  && !res.IsCanceled);
                    break;
                case "isCanceled":
                    res = _context.Reservation
                        .Where(res => userCars.Contains(res.Car.LicensePlate) && res.IsCanceled == true);
                    break;
                case "isOverDue":
                    res= _context.Reservation
                        .Where(res => userCars.Contains(res.Car.LicensePlate) && res.IsOverdue);
                    break;
            };
            if (res == null){
                return null;
            }
            return res.Select(res => new ReservationDTO
            {
                resId = res.ResId,
                resTime = (DateTime)res.ResTime,
                lotName = res.Lot.LotName,
                district = res.Lot.District,
                location = res.Lot.Location,
                licensePlate = res.Car.LicensePlate,
                startTime = (DateTime)res.StartTime,
                validUntil = (DateTime)res.ValidUntil,  //還在進行中的預定要顯示
                PaymentStatus = res.PaymentStatus,
                isCanceled = res.IsCanceled,
                isOverdue = res.IsOverdue, //判斷是否逾時(違規)
                isFinish = res.IsFinish,
                latitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Latitude).FirstOrDefault(),
                longitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Longitude).FirstOrDefault(),
                lotId = res.Lot.LotId, //為了要導入到預定頁面需要停車場id
            });
        }


        //取消預訂(isCancel和isFinish都要變)
        //PUT: api/Reservations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(int id)
        {
            var updateRes = await _context.Reservation.FindAsync(id);
            if (updateRes == null)
            {
                return BadRequest(new { Message = "取消預訂失敗，查無此預訂資料" });
            }
            else if(updateRes.PaymentStatus == false)
            {
                updateRes.IsCanceled = true;
                updateRes.IsFinish = true;  //要設為完成，取消會顯示在前端已結案區塊
            }
            else
            {
                updateRes.IsCanceled =true;
                updateRes.IsFinish = true;  //要設為完成，取消會顯示在前端已結案區塊
                var updateLot = await _context.ParkingLots.FindAsync(updateRes.LotId);
                updateLot.ValidSpace += 1; //釋放車位 
            };

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { Message = "已取消預訂", Success = true });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Ok(new { Message = "取消預訂失敗", Success = false });
            };
            

        }

        [HttpGet("GetLotsInfo")]
        public async Task<IActionResult> GetLotsInfo(int lotId)
        {
            var lotInfo = await _context.ParkingLots.Where(lot => lot.LotId == lotId).Select(lot => new LotsInfoDTO
            {
                LotId = lot.LotId,
                LotName = lot.LotName,
                Location = lot.Location,
                Latitude = lot.Latitude,
                Longitude = lot.Longitude,
                SmallCarSpace = lot.SmallCarSpace,
                MonRentalSpace = lot.MonRentalSpace,
                EtcSpace = lot.EtcSpace,
                MotherSpace = lot.MotherSpace,
                RateRules = lot.RateRules,
                WeekdayRate = lot.WeekdayRate,
                HolidayRate = lot.HolidayRate,
                MonRentalRate = lot.MonRentalRate,
                ResDeposit = lot.ResDeposit,
                Tel = lot.Tel,
                ValidSpace = lot.ValidSpace,
                LotImages = _context.ParkingLotImages.Where(img => img.LotId == lotId).Select(img => img.ImgPath).ToList()
            }).FirstOrDefaultAsync();
            if (lotInfo == null)
            {
                return NotFound(new { Message = "無此停車場" });
            }
            return Ok(lotInfo);
        }

        [HttpGet("GetUserCarPlate")]
        public async Task<ActionResult<List<Car>>> GetUserCarPlate(int userId)
        {
            var userCarPlate = await _context.Car.Where(c => c.UserId == userId).Select(c => c.LicensePlate).ToListAsync();
            var carId = await _context.Car.Where(c => c.UserId == userId).Select(c => c.CarId).ToListAsync();
            if (userCarPlate.Count == 0 && carId == null)
            {
                return NotFound(new { Message = "無任何車輛" });
            }
            return Ok(new { carId, userCarPlate });
        }

        // POST: api/ResService
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("newReservation")]
        public async Task<IActionResult> PostReservation([FromBody] ReservationDTO resDTO, [FromQuery] int userId)
        {
            try
            {
                var user = await _context.Customer.FindAsync(userId);
                if(user != null && user.IsBlack == true)
                {
                    return BadRequest(new { Message = "黑名單用戶無法進行預約" });
                }

                if (userId == null)
                {
                    return BadRequest(new { Message = "無法取得用戶ID" });
                }

                var exitingRes = await _context.Reservation.Where(res => res.Car.LicensePlate == resDTO.licensePlate && !res.IsFinish && !res.IsCanceled && res.PaymentStatus == false).FirstOrDefaultAsync();
                if (exitingRes != null)
                {
                    return BadRequest(new { Message = "您有未完成的預約，請先完成付款或取消" });
                }

                var CarInLot = await (from entry in _context.EntryExitManagement
                                      where entry.Car.LicensePlate == resDTO.licensePlate && entry.PaymentStatus == false
                                      select entry).FirstOrDefaultAsync();
                if (CarInLot != null)
                {
                    return BadRequest(new { Message = "您有未付款的進場紀錄，請先完成付款或離場" });
                }

                var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                var taiwanTime = TimeZoneInfo.ConvertTime(DateTime.Now, taiwanTimeZone);
                if((resDTO.startTime - taiwanTime).TotalHours > 6)
                {
                    return BadRequest(new { Message = "預約時間不能大於現在時間6小時" });
                }
                var reservationService = new ReservationService(_context);
                var newReservation = await reservationService.CreateReservationAysnc(resDTO, userId);
                var result = new
                {
                    Message = "預約成功!",
                    newRes = new
                    {
                        newReservation.CarId,
                        newReservation.LotId,
                        newReservation.ResTime,
                        newReservation.StartTime,
                        newReservation.ValidUntil,
                    }
                };

                return Ok(result);
            
            }
            catch (Exception ex)
            {
                // 捕捉所有異常並返回自訂錯誤訊息
                if (ex.Message.Contains("不提供預約服務"))
                {
                    return Ok(new { Message = "該停車場不提供預約服務", Success = false });
                }
                else if (ex.Message.Contains("車位已滿"))
                {
                    return Ok(new { Message = "車位已滿, 無法進行預約", Success = false });
                }
                else if (ex.Message.Contains("無效的停車場"))
                {
                    return Ok(new { Message = "無效的停車場",  Success = false});
                }
                else if (ex.Message.Contains("該車牌不屬於當前用戶"))
                {
                    return Ok(new { Message = "該車牌不屬於當前用戶", Succes = false });
                }

                // 捕捉所有異常並返回具體錯誤訊息
                return StatusCode(500, new { Message = "伺服器內部錯誤", Detail = ex.Message });
            }
        }

        //------------line bot使用

        //查詢還在進行中的預訂資訊
        // GET: api/Reservations/CurrentReservations
        [HttpGet("CurrentReservations")]
        public async Task<IEnumerable<ReservationDTO>> GetCurrentReservation(int userId)
        {

            //篩選該用戶車牌的預訂資料(還在進行中的預訂)
            var reservations = _context.Reservation
                .Where(res => res.Car.UserId == userId && !res.IsFinish && !res.IsCanceled)
                .Select(res => new ReservationDTO
                {
                    resId = res.ResId,
                    resTime = (DateTime)res.ResTime,  //預訂時間
                    lotName = res.Lot.LotName,  //停車場名稱
                    location = res.Lot.Location,
                    licensePlate = res.Car.LicensePlate,
                    startTime = (DateTime)res.StartTime,  //預訂進場時間
                    validUntil = (DateTime)res.ValidUntil,  //最遲進場時間
                    PaymentStatus = res.PaymentStatus,

                    //下面三個狀態其實line不需要
                    isCanceled = res.IsCanceled,
                    isOverdue = res.IsOverdue, //判斷是否逾時(違規)
                    isFinish = res.IsFinish,
                    //-----
                    latitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Latitude).FirstOrDefault(),
                    longitude = _context.ParkingLots.Where(lot => lot.LotName == res.Lot.LotName).Select(lot => lot.Longitude).FirstOrDefault(),
                    lotId = res.Lot.LotId, //為了要導入到預定頁面需要停車場id

                });
            if (reservations == null)
            {
                return null;
            }
            return reservations;
        }


        private bool ReservationExists(int id)
        {
            return _context.Reservation.Any(e => e.ResId == id);
        }
    }


}
