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
    public class MyPayService
    {
        private readonly EasyParkContext _context;
        public MyPayService(EasyParkContext context)
        {
            _context = context;
        }
        //--------------------- 月租方案開始 ------------------------

        public MonthlyRental MapDtoToModel(PaymentRequestDto dto)
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
                Amount = dto.Amount,
                PaymentStatus = false,
                TransactionId = dto.OrderId
            };
        }
        //--------------------- 月租方案結束 ------------------------

        //------------------ 檢測月租方案是否相符開始 ---------------------
        public async Task<bool> ValidatePayment(int LotId ,string planId, int paidAmount)
        {
            var park = await _context.ParkingLots
                .FirstOrDefaultAsync(r => r.LotId == LotId);
            
            if (park == null)
            {
                throw new Exception("此停車場並不存在");
            }
            var (rentalMonths, discount) = planId switch
            {
                "oneMonth" => (1, 0),           // 無折扣
                "threeMonths" => (3, 0.10m),      // 5% 折扣
                "sixMonths" => (6, 0.15m),        // 10% 折扣
                "twelveMonths" => (12, 0.20m),    // 15% 折扣
                _ => throw new ArgumentException("無效的方案 ID")
            };

            // 根據租賃月數計算未折扣的總金額
            decimal originalAmount = park.MonRentalRate * rentalMonths;

            // 計算折扣後的金額
            decimal discountedAmount = originalAmount * (1 - discount);

            // 驗證支付的金額是否正確
            if (Math.Round(discountedAmount) != paidAmount) // 四捨五入取整數
            {
                Console.WriteLine($"{park.LotName} 的方案 {planId} 金額比對錯誤：應支付 {Math.Round(discountedAmount)}，實際支付 {paidAmount}");
                return false;
            }

            Console.WriteLine($"{park.LotName} 的方案 {planId} 金額比對成功：支付金額 {paidAmount}");
            return true;

        }
        //------------------ 檢測月租方案是否相符結束 ---------------------


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


        //------------------ 檢測預定金額和每個小時的時間是否相符開始 ---------------------------

        public async Task<bool> ValidateDayMoney(int lotId, int weekDay)
        {
            var park = await _context.ParkingLots
                .FirstOrDefaultAsync(r => r.LotId == lotId);
            if (park == null)
            {
                throw new Exception("此停車場並不存在");

            }
            if (park.WeekdayRate != weekDay)
            {
                Console.WriteLine($"{park.LotName}的每小時金額比對錯誤");
                return false;
            }
            else
            {
                return true;
            }

        }
        //------------------ 檢測預定金額和每個小時的時間是否相符結束 --------------------------- 

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
                var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                var currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, taipeiTimeZone);
                rentalRecord.PaymentStatus = true;
                parkLot.ValidSpace -= 1;

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

        public Reservation ResMapDtoToModel(PaymentRequestDto dto)
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
                ResTime= currentTime,
                ValidUntil = vaildTime,
                StartTime = dto.StartTime,
                PaymentStatus = false,
                TransactionId = dto.OrderId
            };

        }
        //--------------------- 預約表單結束 ------------------------

        // ------------------------ 驗證預約停車的金額是否相符開始 -------------------------------
        public async Task<(bool IsValid, string Message)> ValidateDayPaymentAsync(PaymentValidationDayDto dto)
        {
            // 1. 根據 lotId 查找對應的停車場，取得 WeekdayRate
            var parkingLot = await _context.ParkingLots
                .FirstOrDefaultAsync(lot => lot.LotId == dto.LotId);

            if (parkingLot == null)
            {
                return (false, "找不到對應的停車場資料。");
            }

            // 2. 根據 carId 查找 entryExitRecord 並設定 LicensePlateKeyinTime
            var entryExitRecord = await _context.EntryExitManagement
                .FirstOrDefaultAsync(record => record.CarId == dto.carId && record.PaymentStatus == false);

            if (entryExitRecord == null)
            {
                return (false, "未找到該車輛的進出記錄。");
            }

            var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var taiwanTime = TimeZoneInfo.ConvertTime(DateTime.Now, taiwanTimeZone);
            // 設定 LicensePlateKeyinTime 為目前時間
            entryExitRecord.LicensePlateKeyinTime = taiwanTime;

            // 3. 計算停留時間（小時）
            TimeSpan? duration = entryExitRecord.LicensePlateKeyinTime - entryExitRecord.EntryTime;

            if (!duration.HasValue)
            {
                return (false, "無法計算停留時間。");
            }

            var durationHours = (int)Math.Ceiling(duration.Value.TotalHours); // 進位處理小時數

            // 4. 使用 WeekdayRate 計算原始金額
            var originalAmount = durationHours * parkingLot.WeekdayRate;

            // 5. 如果有優惠券，根據 couponsId 查找折扣金額
            decimal discountAmount = 0;
            if (dto.couponsId != null)
            {
                var coupon = await _context.Coupon
                    .FirstOrDefaultAsync(c => c.CouponId == dto.couponsId);

                if (coupon != null)
                {
                    discountAmount = (decimal)(coupon.DiscountAmount ?? 0);
                }
            }

            // 6. 計算最終金額
            var finalAmount = originalAmount - discountAmount;

            // 如果最終金額小於 0，強制設定為 0
            if (finalAmount <= 0)
            {
                finalAmount = 0;
            }

            // 7. 驗證傳入的金額是否正確
            if (finalAmount != dto.Amount)
            {
                Console.WriteLine("檢測的金額為:" + finalAmount);
                return (false, $"金額不正確。{finalAmount},{originalAmount},{discountAmount}");
            }
            Console.WriteLine("檢測的金額為:" + finalAmount);
            // 8. 返回成功訊息
            return (true, "方案驗證成功。");
        }
        // ------------------------ 驗證預約停車的金額是否相符結束 -------------------------------

        //-------------------------- 建立出場繳費記錄開始 ----------------------------------------

        public EntryExitManagement EntryExitDtoToModel(PaymentRequestDto dto)
        {
            var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, taipeiTimeZone);
            return new EntryExitManagement
            {
                Amount = dto.Amount,
                LicensePlateKeyinTime= currentTime
            };

        }

        //-------------------------- 建立出場繳費記錄結束 ----------------------------------------

        //---------------------------- 繳費成功開始 ---------------------------------
        public async Task<bool> UpdateEntryExitPaymentAsync(UpdateEntryExitPaymenDTO dto)
        {
            // 1. 根據 MycarId 找到 EntryExitManagement 中最後一筆紀錄
            var entryExitRecord = await _context.EntryExitManagement
                .Where(e => e.CarId == dto.MycarId)
                .OrderByDescending(e => e.EntryTime)
                .FirstOrDefaultAsync();

            if (entryExitRecord == null)
            {
                return false; // 找不到紀錄
            }
            var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, taipeiTimeZone);

            // 2. 更新該紀錄的支付資訊
            entryExitRecord.PaymentTime = currentTime;
            entryExitRecord.PaymentStatus = true;
            entryExitRecord.ValidTime = entryExitRecord.PaymentTime?.AddMinutes(15);

            // 3. 如果有 MycouponId，則更新該 Coupon 的使用狀態
            if (dto.MycouponId.HasValue)
            {
                var coupon = await _context.Coupon
                    .FirstOrDefaultAsync(c => c.CouponId == dto.MycouponId.Value);

                if (coupon != null)
                {
                    coupon.IsUsed = true;
                }
            }
            // 4. 在 DealRecord 中新增一筆交易記錄
            var newDealRecord = new DealRecord
            {
                CarId = dto.MycarId,
                Amount = dto.Myamount,
                PaymentTime = currentTime,
                ParkType = "EntryExit"
            };
            await _context.DealRecord.AddAsync(newDealRecord);

            // 5. 保存變更到資料庫
            await _context.SaveChangesAsync();

            return true;
        }
        //---------------------------- 繳費成功結束 ---------------------------------

        //-------------------------- 比對是否有在停車場 ------------------------------
        public async Task<object> FindMyParkingAsync(ListenCarDTO dto)
        {
            // 1. 根據 licensePlate 查找對應的 Car 物件
            var car = await _context.Car.FirstOrDefaultAsync(c => c.LicensePlate == dto.licensePlate);

            if (car == null)
            {
                return new { success = false, message = "未找到該車輛。" };
            }

            // 2. 根據 CarId 查找 EntryExitManagement 中未支付的最新記錄
            var entryExitRecord = await _context.EntryExitManagement
                .Where(eem => eem.CarId == car.CarId && !eem.PaymentStatus) // 加入未支付的條件
                .OrderByDescending(eem => eem.EntryTime)
                .FirstOrDefaultAsync();

            if (entryExitRecord == null)
            {
                return new { success = false, message = "未找到該車輛的有效出入管理記錄。" };
            }

            // 3. 根據 LotId 取得對應的停車場費率 (WeekdayRate)
            var parkingLot = await _context.ParkingLots.FirstOrDefaultAsync(lot => lot.LotId == entryExitRecord.LotId);

            if (parkingLot == null)
            {
                return new { success = false, message = "找不到對應的停車場資料。" };
            }

            var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, taipeiTimeZone);
            // 4. 設定 LicensePlateKeyinTime 為目前時間
            entryExitRecord.LicensePlateKeyinTime = currentTime;

            // 5. 計算停留時間（小時）
            var duration = entryExitRecord.LicensePlateKeyinTime - entryExitRecord.EntryTime;

            if (!duration.HasValue)
            {
                return new { success = false, message = "無法計算停留時間。" };
            }

            var durationHours = (int)Math.Ceiling(duration.Value.TotalHours); // 進位處理小時數

            // 6. 使用停車場的 WeekdayRate 計算金額
            var amount = durationHours * parkingLot.WeekdayRate;

            var coupons = await _context.Coupon
                .Where(c => c.UserId == car.UserId &&
                            !c.IsUsed &&
                            currentTime >= c.ValidFrom &&
                            currentTime <= c.ValidUntil)
                .OrderBy(c => c.ValidUntil)
                .ToListAsync();

            var couponIds = coupons.Select(c => new
            {
                CouponId = c.CouponId,
                couponAmount = c.DiscountAmount,
                EndTime = c.ValidUntil.Date.ToString("yyyy-MM-dd")
            }).ToList();

            // 8. 回傳結果
            return new
            {
                success = true,
                lotId = parkingLot.LotId,
                lotName = parkingLot.LotName,
                CarId = car.CarId,
                LicensePlate = car.LicensePlate,
                EntryTime = entryExitRecord.EntryTime?.ToString("yyyy-MM-dd HH:mm") ?? "N/A",
                LicensePlateKeyinTime = entryExitRecord.LicensePlateKeyinTime?.ToString("yyyy-MM-dd HH:mm") ?? "N/A",
                DurationHours = durationHours,
                PlateAmount = amount,
                couponIds
            };
        }
    }
}
