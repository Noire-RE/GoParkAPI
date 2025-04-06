using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Services;
using Hangfire;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ECPayController : ControllerBase
    {
        private const string HashKey = "pwFHCqoQZGmho4w6";
        private const string HashIV = "EkRm7iFT261dpevs";
        private readonly EasyParkContext _context;
        private readonly ECService _ecService;
        private readonly MailService _sentmail;
        private readonly IConfiguration _configuration;
        private readonly string _ngrokBaseUrl;
        private readonly PushNotificationService _pushNotification;

        public ECPayController(EasyParkContext context, ECService ecService, MailService sentmail, IConfiguration configuration, PushNotificationService pushNotification)
        {
            _context = context;
            _ecService = ecService;
            _sentmail = sentmail;
            _configuration = configuration;
            // 從配置中讀取 ngrokBaseUrl
            _ngrokBaseUrl = _configuration["NgrokSettings:BaseUrl"];
            _pushNotification = pushNotification;
        }

        // 1. 生成 ECPay 月租表單
        [HttpPost("ECPayForm")]
        public async Task<IActionResult> CreateECPayForm([FromBody] ECpayDTO dto)
        {
            var lot = await _context.ParkingLots.FirstOrDefaultAsync(p => p.LotId == dto.LotId);
            if (lot == null) return BadRequest("無效的停車場");

            var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var taiwanTime = TimeZoneInfo.ConvertTime(DateTime.Now, taiwanTimeZone);

            var merchantTradeNo = "MyGo" + taiwanTime.ToString("yyyyMMddHHmmss");
            dto.OrderId = merchantTradeNo;

            // 構建支付參數
            var paymentParameters = new Dictionary<string, string>
            {
                { "MerchantID", "3002607" },
                { "MerchantTradeNo", merchantTradeNo },
                { "MerchantTradeDate", taiwanTime.ToString("yyyy/MM/dd HH:mm:ss") },
                { "PaymentType", "aio" },
                { "TotalAmount", $"{dto.TotalAmount}" },
                { "TradeDesc", dto.ItemName },
                { "ItemName", $"{lot.LotName}({dto.ItemName}) - {dto.PlanName}" },
                { "ReturnURL", $"{_ngrokBaseUrl}/api/ECPay/Callback"  },
                { "ClientBackURL", $"{dto.ClientBackURL}?Pay=Monthly&MerchantTradeNo={merchantTradeNo}" },
                { "ChoosePayment", "ALL" }
            };

            // 生成檢核碼並添加到參數
            string checkMacValue = GenerateCheckMacValue(paymentParameters);
            paymentParameters.Add("CheckMacValue", checkMacValue);

            // 保存租賃記錄
            MonthlyRental rentalRecord = _ecService.MapDtoToModel(dto);
            await _context.MonthlyRental.AddAsync(rentalRecord);
            await _context.SaveChangesAsync();

            return Ok(paymentParameters);
        }

        // 2. 生成檢核碼
        private string GenerateCheckMacValue(Dictionary<string, string> parameters)
        {
            var sortedParams = parameters.OrderBy(p => p.Key, StringComparer.Ordinal)
                                         .Select(p => $"{p.Key}={p.Value}")
                                         .ToList();

            string paramString = $"HashKey={HashKey}&" + string.Join("&", sortedParams) + $"&HashIV={HashIV}";
            string urlEncodedString = HttpUtility.UrlEncode(paramString).ToLower();

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(urlEncodedString));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
            }
        }

        [HttpPost("Callback")]
        public async Task<IActionResult> Callback([FromForm] ECpayCallbackDTO callbackData)
        {
            if (callbackData == null || string.IsNullOrEmpty(callbackData.MerchantTradeNo))
                return BadRequest("無效的回傳資料");



            // 查詢 Customer 資料
            var customer = await _context.MonthlyRental
                .Where(m => m.TransactionId == callbackData.MerchantTradeNo)
                .Join(_context.Car, m => m.CarId, c => c.CarId, (m, c) => c.UserId)
                .Join(_context.Customer, userId => userId, cu => cu.UserId, (userId, cu) => cu)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { success = false, message = "找不到對應的使用者。" });
            }



            // 根據回傳的 RtnCode 更新交易狀態
            if (callbackData.RtnCode == "1") // 1 代表交易成功
            {
                // 更新支付狀態
                var (success, message) = await _ecService.UpdatePaymentStatusAsync(callbackData.MerchantTradeNo);

                if (!success)
                {
                    return NotFound(new { success = false, message });
                }


                // 準備佔位符的值
                var placeholders = new Dictionary<string, string>
                {
                    { "username", customer.Username},
                    { "message", "您的月租已確認，感謝您使用 MyGoParking！" }
                };

                // 指定模板路徑
                string templatePath = "EmailTemplate.html";

                // 讀取模板並替換佔位符
                string emailBody = await _sentmail.LoadEmailTemplateAsync(templatePath, placeholders);

                try
                {
                    // 發送郵件
                    await _sentmail.SendEmailAsync(customer.Email, "MyGoParking 通知", emailBody);
                    Console.WriteLine($"成功發送郵件至 {customer.Email}");
                }
                catch (Exception ex)
                {
                    // 捕捉並記錄郵件發送錯誤
                    Console.WriteLine($"發送郵件時發生錯誤: {ex.Message}");
                }

                // 4. 返回成功回應
                return Ok(new { success = true, message = "支付狀態更新成功並已發送通知。" });
            }

            await _context.SaveChangesAsync();

            return Ok("回傳資料處理完成");
        }


        //--------------------------------------------------------------------------------------------------

        [HttpGet("CheckPaymentStatus")]
        public async Task<IActionResult> CheckPaymentStatus([FromQuery] string MerchantTradeNo, [FromQuery] string Pay)
        {
            if (string.IsNullOrEmpty(MerchantTradeNo) || string.IsNullOrEmpty(Pay))
                return BadRequest("無效的交易編號或類型");

            object rentalRecord = null;

            // 根據 Pay 的值查詢對應的表
            if (Pay == "Monthly")
            {
                rentalRecord = await _context.MonthlyRental.FirstOrDefaultAsync(r => r.TransactionId == MerchantTradeNo);
            }
            else if (Pay == "Res")
            {
                rentalRecord = await _context.Reservation.FirstOrDefaultAsync(r => r.TransactionId == MerchantTradeNo);

                //--------------------------------HangFire付款確認後啟動---------------------------------
                // 查詢該車輛的最新預約記錄 (根據 resId 排序並選擇最新的一筆)
                var latestRes = await _context.Reservation
                    .Where(r => r.TransactionId == MerchantTradeNo)
                    .OrderByDescending(r => r.ResId)
                    .Select(r => r.ResId)
                    .FirstOrDefaultAsync();
                //啟動Hangfire CheckAndSendOverdueReminder
                RecurringJob.AddOrUpdate($"OverdueReminder_{latestRes}", () => _pushNotification.CheckAndSendOverdueReminder(latestRes), "*/1 * * * *");
                //--------------------------------HangFire付款確認後啟動---------------------------------

            }
            else
            {
                return BadRequest("無效的支付類型");
            }

            if (rentalRecord == null)
                return NotFound(new { status = "交易不存在" });

            // 檢查交易狀態
            var paymentStatus = rentalRecord.GetType().GetProperty("PaymentStatus")?.GetValue(rentalRecord, null) as bool?;

            return Ok(new { status = paymentStatus == true ? "已支付" : "未支付" });
        }

        //--------------------------------------------------------------------------------------------------
        // 1. 生成 ECPay 預約表單
        [HttpPost("ResECPayForm")]
        public async Task<IActionResult> CreateResECPayForm([FromBody] ECpayDTO dto)
        {
            var lot = await _context.ParkingLots.FirstOrDefaultAsync(p => p.LotId == dto.LotId);
            if (lot == null) return BadRequest("無效的停車場");

            var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var taiwanTime = TimeZoneInfo.ConvertTime(DateTime.Now, taiwanTimeZone);

            var merchantTradeNo = "MyGo" + taiwanTime.ToString("yyyyMMddHHmmss");
            dto.OrderId = merchantTradeNo;


            // 構建支付參數
            var paymentParameters = new Dictionary<string, string>
            {
                { "MerchantID", "3002607" },
                { "MerchantTradeNo", merchantTradeNo },
                { "MerchantTradeDate", taiwanTime.ToString("yyyy/MM/dd HH:mm:ss") },
                { "PaymentType", "aio" },
                { "TotalAmount", $"{dto.TotalAmount}" },
                { "TradeDesc", dto.ItemName },
                { "ItemName", $"{lot.LotName}({dto.ItemName}) - {dto.PlanName}" },
                { "ReturnURL",  $"{_ngrokBaseUrl}/api/ECPay/ResCallback" },
                { "ClientBackURL", $"{dto.ClientBackURL}?Pay=Res&MerchantTradeNo={merchantTradeNo}" },
                { "ChoosePayment", "ALL" }
            };

            // 生成檢核碼並添加到參數
            string checkMacValue = GenerateCheckMacValue(paymentParameters);
            paymentParameters.Add("CheckMacValue", checkMacValue);

            // 保存租賃記錄
            Reservation rentalRecord = _ecService.ResMapDtoToModel(dto);
            await _context.Reservation.AddAsync(rentalRecord);
            await _context.SaveChangesAsync();

            return Ok(paymentParameters);
        }
        //-------------------------------------預約回傳------------------------------------------------
        [HttpPost("ResCallback")]
        public async Task<IActionResult> ResCallback([FromForm] ECpayCallbackDTO callbackData)
        {
            if (callbackData == null || string.IsNullOrEmpty(callbackData.MerchantTradeNo))
                return BadRequest("無效的回傳資料");



            // 查詢 Customer 資料
            var customer = await _context.Reservation
                .Where(m => m.TransactionId == callbackData.MerchantTradeNo)
                .Join(_context.Car, m => m.CarId, c => c.CarId, (m, c) => c.UserId)
                .Join(_context.Customer, userId => userId, cu => cu.UserId, (userId, cu) => cu)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { success = false, message = "找不到對應的使用者。" });
            }



            // 根據回傳的 RtnCode 更新交易狀態
            if (callbackData.RtnCode == "1") // 1 代表交易成功
            {
                // 更新支付狀態
                var (success, message) = await _ecService.UpdateResPayment(callbackData.MerchantTradeNo);

                if (!success)
                {
                    return NotFound(new { success = false, message });
                }


                // 準備佔位符的值
                var placeholders = new Dictionary<string, string>
                {
                    { "username", customer.Username},
                    { "message", "您的預約已確認，感謝您使用 MyGoParking！" }
                };

                // 指定模板路徑
                string templatePath = "EmailTemplate.html";

                // 讀取模板並替換佔位符
                string emailBody = await _sentmail.LoadEmailTemplateAsync(templatePath, placeholders);

                try
                {
                    // 發送郵件
                    await _sentmail.SendEmailAsync(customer.Email, "MyGoParking 通知", emailBody);
                    Console.WriteLine($"成功發送郵件至 {customer.Email}");
                }
                catch (Exception ex)
                {
                    // 捕捉並記錄郵件發送錯誤
                    Console.WriteLine($"發送郵件時發生錯誤: {ex.Message}");
                }

                // 4. 返回成功回應
                return Ok(new { success = true, message = "支付狀態更新成功並已發送通知。" });
            }

            await _context.SaveChangesAsync();

            return Ok("回傳資料處理完成");
        }

        //---------------------------繳費---------------------------------------------------------
        // 创建静态字典缓存 MerchantTradeNo 和 EntryexitId 的映射
        private static readonly Dictionary<string, int> _merchantTradeNoCache = new Dictionary<string, int>();

        [HttpPost("ChargeCreate")]
        public async Task<IActionResult> ChargeCreate([FromBody] ECpayDTO dto)
        {
            // 根据 CarId 和 LotId 查找現有的進出記錄
            var existingRecord = await _context.EntryExitManagement
                    .OrderByDescending(e => e.EntryexitId)
                    .FirstOrDefaultAsync(e => e.CarId == dto.CarId && e.LotId == dto.LotId);

            if (existingRecord == null)
            {
                return BadRequest(new { success = false, message = "未找到該車輛的進出記錄。" });
            }

            var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var taiwanTime = TimeZoneInfo.ConvertTime(DateTime.Now, taiwanTimeZone);

            // 查找 LotName
            var ParkName = await _context.ParkingLots.FirstOrDefaultAsync(l => l.LotId == dto.LotId);
            var merchantTradeNo = "MyGo" + taiwanTime.ToString("yyyyMMddHHmmss");
            dto.OrderId = merchantTradeNo;

            // 存储 MerchantTradeNo 和 EntryexitId 的映射到缓存中
            _merchantTradeNoCache[merchantTradeNo] = existingRecord.EntryexitId;

            // 构建支付参数
            var paymentParameters = new Dictionary<string, string>
            {
                { "MerchantID", "3002607" },
                { "MerchantTradeNo", merchantTradeNo },
                { "MerchantTradeDate", taiwanTime.ToString("yyyy/MM/dd HH:mm:ss") },
                { "PaymentType", "aio" },
                { "TotalAmount", $"{dto.TotalAmount}" },
                { "TradeDesc", dto.ItemName },
                { "ItemName", $"{ParkName.LotName}-{dto.ItemName}" },
                { "ReturnURL",  $"{_ngrokBaseUrl}/api/ECPay/ChargeCallback" },
                { "ClientBackURL", $"{dto.ClientBackURL}?MerchantTradeNo={merchantTradeNo}" },
                { "ChoosePayment", "ALL" }
             };
            //檢核碼並加到參數
            string checkMacValue = GenerateCheckMacValue(paymentParameters);
            paymentParameters.Add("CheckMacValue", checkMacValue);

            // 更新現在出場記錄時間和支付金額
            existingRecord.LicensePlateKeyinTime = taiwanTime;
            existingRecord.Amount = dto.TotalAmount;
            _context.EntryExitManagement.Update(existingRecord);
            await _context.SaveChangesAsync();

            return Ok(paymentParameters);
        }

        // 回调方法中比对 MerchantTradeNo
        [HttpPost("ChargeCallback")]
        public async Task<IActionResult> ChargeCallback([FromForm] ECpayCallbackDTO callbackData)
        {
            if (callbackData == null || string.IsNullOrEmpty(callbackData.MerchantTradeNo))
                return BadRequest("無效的回傳資料");

            if (_merchantTradeNoCache.TryGetValue(callbackData.MerchantTradeNo, out int entryexitId))
            {
                // 根据 entryexitId 查找 EntryExitManagement 记录
                var entryExitRecord = await _context.EntryExitManagement
                    .FirstOrDefaultAsync(e => e.EntryexitId == entryexitId);

                if (entryExitRecord == null)
                    return NotFound("找不到對應的出入紀錄");

                // 如果交易成功，直接更新 PaymentStatus 为 true
                if (callbackData.RtnCode == "1") // 1 代表交易成功
                {
                    entryExitRecord.PaymentStatus = true;
                    // 將當前時間轉為台北時間
                    var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                    entryExitRecord.PaymentTime = TimeZoneInfo.ConvertTime(DateTime.Now, taipeiTimeZone);
                    entryExitRecord.ValidTime = entryExitRecord.PaymentTime.Value.AddMinutes(15);

                    // 處理完成以後從緩存刪除交易記錄
                    _merchantTradeNoCache.Remove(callbackData.MerchantTradeNo);
                }
            }
            else
            {
                return NotFound("找不到對應的交易映射");
            }

            await _context.SaveChangesAsync();

            return Ok("回傳資料處理完成");
        }

        //-------------------------------------------------------------------------------------------------------

        [HttpPost("ConfirmPayment")]
        public async Task<IActionResult> ConfirmPayment(UpdateEntryExitPaymenDTO dto)
        {
            // 檢查是否有相同的 CarId 和 Amount 且在 1 分鐘內的交易
            var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, taipeiTimeZone);
            var oneMinuteAgo = currentTime.AddMinutes(-1);

            var existingRecord = await _context.DealRecord
                .AnyAsync(record => record.CarId == dto.MycarId &&
                                    record.Amount == dto.Myamount &&
                                    record.PaymentTime >= oneMinuteAgo &&
                                    record.PaymentTime <= currentTime &&
                                    record.ParkType == "EntryExit");

            // 若存在相同的交易記錄，則直接返回
            if (existingRecord)
            {
                return BadRequest("重複的交易記錄，操作已取消。");
            }

            // 若使用了優惠券，更新優惠券狀態
            if (dto.MycouponId.HasValue)
            {
                var coupon = await _context.Coupon
                    .FirstOrDefaultAsync(c => c.CouponId == dto.MycouponId.Value);

                if (coupon != null)
                {
                    coupon.IsUsed = true;
                }
            }

            // 新增交易記錄到 DealRecord
            var newDealRecord = new DealRecord
            {
                CarId = dto.MycarId,
                Amount = dto.Myamount,
                PaymentTime = currentTime,
                ParkType = "EntryExit"
            };
            await _context.DealRecord.AddAsync(newDealRecord);

            // 保存變更到資料庫
            await _context.SaveChangesAsync();

            return Ok("付款確認完成，交易已記錄。");
        }

    }

}


