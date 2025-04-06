using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Providers;
using Humanizer;
using MailKit.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Text;
using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;

namespace GoParkAPI.Services
{
    public class ECService
    {
        private readonly EasyParkContext _context;
        public ECService(EasyParkContext context)
        {
            _context = context;
        }
        //--------------------- 月租方案開始 ------------------------

        public MonthlyRental MapDtoToModel(ECpayDTO dto)
        {
            // 根據方案 ID 動態設置結束日期
            int rentalMonths = dto.PlanId switch
            {
                "oneMonth" => 1,
                "threeMonths" => 3,
                "sixMonths" => 6,
                "twelveMonths" => 12,
                _ => throw new ArgumentException("Invalid PlanId")
            };

            // 確保 StartTime 有值，如果為 null 則預設為今天
            DateTime startTime = dto.StartTime ?? DateTime.Today;

            // 將 EndDate 設置為 StartTime 加上租賃月數
            return new MonthlyRental
            {
                CarId = dto.CarId,
                LotId = dto.LotId,
                StartDate = startTime,
                EndDate = startTime.AddMonths(rentalMonths),
                Amount = dto.TotalAmount,
                PaymentStatus = false,
                TransactionId = dto.OrderId
            };
        }
        //--------------------- 月租方案結束 ------------------------


        //------------------ 月租支付完成表單建立開始---------------------------------
        public async Task<(bool success, string message)> UpdatePaymentStatusAsync(string orderId)
        {
            var rentalRecord = await _context.MonthlyRental
                .FirstOrDefaultAsync(r => r.TransactionId == orderId);

            if (rentalRecord == null)
            {
                return (false, "找不到對應的租賃紀錄");
            }

            var parkLot = await _context.ParkingLots
                .FirstOrDefaultAsync(p => p.LotId == rentalRecord.LotId);

            if (parkLot == null)
            {
                return (false, "找不到對應的車位");
            }

            if (parkLot.MonRentalSpace <= 0 || parkLot.ValidSpace <= 0)
            {
                return (false, "車位不足，無法更新支付狀態");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                rentalRecord.PaymentStatus = true;
                parkLot.MonRentalSpace -= 1;
                var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                var currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, taipeiTimeZone);
                var dealRecord = new DealRecord
                {
                    CarId = rentalRecord.CarId,
                    Amount = rentalRecord.Amount,
                    PaymentTime = currentTime,
                    ParkType = "monthlyRental"
                };

                await _context.DealRecord.AddAsync(dealRecord);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "支付狀態已更新");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine("更新支付狀態失敗");
                return (false, "更新支付狀態失敗，請稍後再試");
            }
        }

        //------------------ 月租支付完成表單建立開始---------------------------------


        //------------------ 預約支付完成表單建立開始---------------------------------
        public async Task<(bool success, string message)> UpdateResPayment(string orderId)
        {
            var rentalRecord = await _context.Reservation
                .FirstOrDefaultAsync(r => r.TransactionId == orderId);

            if (rentalRecord == null)
            {
                return (false, "找不到對應的租賃紀錄");
            }

            var parkLot = await _context.ParkingLots
                .FirstOrDefaultAsync(p => p.LotId == rentalRecord.LotId);

            if (parkLot == null)
            {
                return (false, "找不到對應的車位");
            }

            if (parkLot.ValidSpace <= 0)
            {
                return (false, "車位不足，無法更新支付狀態");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                rentalRecord.PaymentStatus = true;
                parkLot.ValidSpace -= 1;
                var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                var currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, taipeiTimeZone);
                var dealRecord = new DealRecord
                {
                    CarId = rentalRecord.CarId,
                    Amount = parkLot.ResDeposit,
                    PaymentTime = currentTime,
                    ParkType = "margin"
                };

                await _context.DealRecord.AddAsync(dealRecord);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "支付狀態已更新");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine("更新支付狀態失敗");
                return (false, "更新支付狀態失敗，請稍後再試");
            }
        }

        //------------------ 預約支付完成表單建立開始---------------------------------

        //--------------------- 預約表單開始 ------------------------

        public Reservation ResMapDtoToModel(ECpayDTO dto)
        {
            DateTime startTime = (DateTime)dto.StartTime;
            TimeSpan overTime = TimeSpan.FromMinutes(3);
            DateTime vaildTime = startTime.Add(overTime);
            var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, taipeiTimeZone);
            return new Reservation
            {
                CarId = dto.CarId,
                LotId = dto.LotId,
                ResTime = currentTime,
                ValidUntil = vaildTime,
                StartTime = dto.StartTime,
                PaymentStatus = false,
                TransactionId = dto.OrderId
            };

        }
        //--------------------- 預約表單結束 ------------------------

        

    }
}
