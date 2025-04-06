using GoParkAPI.DTO;
using GoParkAPI.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace GoParkAPI.Services
{
    public class ReservationService
    {
        private readonly EasyParkContext _context;
        public ReservationService(EasyParkContext context)
        {
            _context = context;
        }
        public async Task<Reservation> CreateReservationAysnc(ReservationDTO resDTO, int userId)
        {
            var userCar = await _context.Car.Where(c => c.UserId == userId).Select(c => c.LicensePlate).ToListAsync();

            if(!userCar.Contains(resDTO.licensePlate))
            {
                throw new Exception("該車牌不屬於當前用戶");
            }

            var parkingLots = await _context.ParkingLots.FirstOrDefaultAsync(lot => lot.LotName == resDTO.lotName) ?? throw new Exception("無效的停車場");

            if(parkingLots.ResDeposit <= 0)
            {
                throw new Exception("該停車場不提供預約服務");
            }

            if(parkingLots.ValidSpace <= 0)
            {
                throw new Exception("車位已滿");
            }

            //計算Valid_until
            DateTime startTime = resDTO.startTime;
            TimeSpan overdueTime = TimeSpan.FromMinutes(15);
            DateTime validUntil = startTime.Add(overdueTime);

            var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var taiwanTime = TimeZoneInfo.ConvertTime(DateTime.Now, taiwanTimeZone);

            //創建新預約
            var newRes = new Reservation
            {
                CarId = _context.Car.FirstOrDefault(c => c.LicensePlate == resDTO.licensePlate).CarId,
                LotId = parkingLots.LotId,
                ResTime = taiwanTime,
                StartTime = startTime,
                ValidUntil = validUntil, //保存計算結果
            };

            return newRes;
        }
    }
}
