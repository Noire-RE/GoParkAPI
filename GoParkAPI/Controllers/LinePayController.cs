using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Services;
using Hangfire;
using Humanizer;
using MailKit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Security.Policy;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinePayController : ControllerBase
    {
        private readonly LinePayService _linePayService;
        private readonly MyPayService _myPayService;
        private readonly EasyParkContext _context;
        private readonly MailService _sentmail;
        private readonly PushNotificationService _pushNotification;
        private readonly IHostEnvironment _enviroment;
        public LinePayController(LinePayService linePayService, EasyParkContext context, MailService sentmail, MyPayService myPayService, PushNotificationService pushNotification, IHostEnvironment enviroment)
        {
            _linePayService = linePayService;
            _context = context;
            _sentmail = sentmail;
            _myPayService = myPayService;
            _pushNotification = pushNotification;
            _enviroment = enviroment;
        }

        // ------------------------ 驗證月租方案是否相符開始 -------------------------------

        [HttpPost("Validate")]
        public async Task<IActionResult> ValidatePayment([FromBody] PaymentValidationDto dto)
        {
            try
            {
                // 驗證基本資料
                if (dto.LotId <= 0 || string.IsNullOrEmpty(dto.PlanId) || dto.Amount <= 0)
                {
                    return BadRequest(new { message = "無效的停車場 ID、方案 ID 或金額。" });
                }

                // 呼叫服務層進行驗證
                bool isValid = await _myPayService.ValidatePayment(dto.LotId, dto.PlanId, dto.Amount);

                if (!isValid)
                {
                    return BadRequest(new { message = "方案或金額驗證失敗。" });
                }

                return Ok(new { isValid = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"伺服器錯誤: {ex.Message}");
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }
        }

        // ------------------------ 驗證月租方案是否相符結束 -------------------------------

        // ------------------------ 發送月租付款申請開始 -------------------------------

        [HttpPost("Create")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentRequestDto dto)
        {
            try
            {
                // 使用 LinePay 服務發送支付請求
                var paymentResponse = await _linePayService.SendPaymentRequest(dto);

                // 從資料庫查詢該 CarId 的所有租賃記錄
                var existingRentals = await _context.MonthlyRental
                    .Where(r => r.CarId == dto.CarId)
                    .ToListAsync();

                // 將 DTO 映射為 MonthlyRental 模型
                MonthlyRental rentalRecord = _myPayService.MapDtoToModel(dto);

                // 將租賃記錄新增到資料庫
                await _context.MonthlyRental.AddAsync(rentalRecord);

                // 保存變更
                await _context.SaveChangesAsync();

                // 回傳支付結果
                return Ok(paymentResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"發生錯誤：{ex.Message}");

                // 回傳錯誤回應
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "處理支付時發生錯誤。" });
            }

        }

        // ------------------------ 發送月租付款申請結束 -------------------------------

        // ------------------------ 完成月租付並建立付款記錄開始 -------------------------------


        [HttpPost("UpdatePaymentStatus")]
        public async Task<IActionResult> UpdatePaymentStatus([FromBody] UpdatePaymentStatusDTO dto)
        {
            // 查詢 Customer 資料
            var customer = await _context.MonthlyRental
                .Where(m => m.TransactionId == dto.OrderId)
                .Join(_context.Car, m => m.CarId, c => c.CarId, (m, c) => c.UserId)
                .Join(_context.Customer, userId => userId, cu => cu.UserId, (userId, cu) => cu)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { success = false, message = "找不到對應的使用者。" });
            }

            // 更新支付狀態
            var (success, message) = await _myPayService.UpdatePaymentStatusAsync(dto.OrderId);

            if (!success)
            {
                return NotFound(new { success = false, message });
            }


            //準備佔位符的值
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


        // ------------------------ 完成月租付並建立付款記錄結束 -------------------------------

        //------------------------- 從前台接收lotId資訊，然後從後台返回資料-------------------------

        [HttpPost("ListenLotId")]
        public async Task<IActionResult> ListenLotId([FromBody] ListenLotDTO dto)
        {
            try
            {

                var lotId = dto.LotId;
                if (lotId == null)
                {
                    return NotFound(new { message = "LotId 不存在於 Session" });
                }

                // 2. 根據 LotId 查詢停車場資訊
                var park = await _context.ParkingLots.FirstOrDefaultAsync(p => p.LotId == lotId);
                if (park == null)
                {
                    return NotFound(new { message = "找不到對應的停車場" });
                }

                // 3. 回傳停車場資訊
                return Ok(new
                {
                    message = "成功取得停車場資訊",
                    lotName = park.LotName,
                    lotType = park.Type,
                    lotLocation = park.Location,
                    lotValid = park.ValidSpace,
                    lotWeek = park.WeekdayRate,
                    lotTel = park.Tel,
                    lotLatitude = park.Latitude,
                    lotLongitude = park.Longitude,
                    lotResDeposit = park.ResDeposit,
                });
            }
            catch (Exception ex)
            {
                // 4. 捕捉例外狀況並回傳 500 錯誤
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }

        }


        //------------------------------ 預約支付請求開始 ------------------------------------

        [HttpPost("CreateDay")]
        public async Task<IActionResult> CreateDay([FromBody] PaymentRequestDto dto)
        {
            try
            {
                // 使用 LinePay 服務發送支付請求
                var paymentResponse = await _linePayService.SendPaymentRequest(dto);

                Reservation rentalRecord = _myPayService.ResMapDtoToModel(dto);

                // 將租賃記錄新增到資料庫
                await _context.Reservation.AddAsync(rentalRecord);

                // 保存變更
                await _context.SaveChangesAsync();

                // 回傳支付結果
                return Ok(paymentResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"發生錯誤：{ex.Message}");

                // 回傳錯誤回應
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "處理支付時發生錯誤。" });
            }

        }

        // ------------------------ 預約支付請求結束 -------------------------------

        //------------------------- 預約完成表單建立開始 -------------------------------

        [HttpPost("UpdateResPayment")]
        public async Task<IActionResult> UpdateResPayment([FromBody] UpdatePaymentStatusDTO dto)
        {
            // 1. 查詢 Customer 資料
            //var customer = await _context.Reservation
            //    .Where(m => m.TransactionId == dto.OrderId)
            //    .Join(_context.Car, m => m.CarId, c => c.CarId, (m, c) => c.UserId)
            //    .Join(_context.Customer, userId => userId, cu => cu.UserId, (userId, cu) => cu)
            //    .FirstOrDefaultAsync();
            var reservation = await _context.Reservation
                .Where(r => r.TransactionId == dto.OrderId)
                .FirstOrDefaultAsync();
            if (reservation == null)
            {
                return NotFound(new { success = false, message = "找不到對應的預約記錄。" });
            }

            var car = await _context.Car
                .Where(c => c.CarId == reservation.CarId)
                .FirstOrDefaultAsync();
            if (car == null)
            {
                return NotFound(new { success = false, message = "找不到對應的車輛記錄。" });
            }

            var customer = await _context.Customer.Where(c => c.UserId == car.UserId).FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { success = false, message = "找不到對應的使用者。" });
            }

            // 2. 更新支付狀態
            var (success, message) = await _myPayService.UpdateResPayment(dto.OrderId);

            if (!success)
            {
                return NotFound(new { success = false, message });
            }
            // 準備佔位符的值
            var placeholders = new Dictionary<string, string>
            {
                { "username", customer.Username},
                { "message", "您的預約已成功，請在約定的時間抵達，感謝您使用 MyGoParking！" }
            };

            // 假設模板文件放在專案根目錄
            //string templatePath = "Templates/EmailTemplate.html";
            var templatePath = Path.Combine(_enviroment.ContentRootPath, "EmailTemplate.html");

            // 指定模板路徑

            // 讀取模板並替換佔位符
            string emailBody = await _sentmail.LoadEmailTemplateAsync(templatePath, placeholders);

            try
            {
                // 發送郵件
                await _sentmail.SendEmailAsync(customer.Email, "MyGoParking 通知", emailBody);

                //--------------------------------HangFire付款確認後啟動---------------------------------
                // 查詢該車輛的最新預約記錄 (根據 resId 排序並選擇最新的一筆)
                var latestRes = await _context.Reservation
                    .Where(r => r.TransactionId == dto.OrderId)
                    .OrderByDescending(r => r.ResId)
                    .Select(r => r.ResId)
                    .FirstOrDefaultAsync();
                //啟動Hangfire CheckAndSendOverdueReminder
                RecurringJob.AddOrUpdate($"OverdueReminder_{latestRes}", () => _pushNotification.CheckAndSendOverdueReminder(latestRes), "*/1 * * * *");
                //--------------------------------HangFire付款確認後啟動---------------------------------

                Console.WriteLine($"成功發送郵件至 {customer.Email}");
            }
            catch (Exception ex)
            {
                // 捕捉並記錄郵件發送錯誤
                Console.WriteLine($"發送郵件時發生錯誤: {ex.Message}");
            }
            // 5. 支付成功回應
            return Ok(new { success = true, message = "支付狀態更新成功並已發送通知。" });
        }


        //------------------------- 預約完成表單建立結束 -------------------------------

        //---------------------------- 停出付款開始 -----------------------------------
        [HttpPost("UpdateEntryExitPayment")]
        public async Task<IActionResult> UpdateEntryExitPayment([FromBody] UpdateEntryExitPaymenDTO dto)
        {
            var result = await _myPayService.UpdateEntryExitPaymentAsync(dto);

            if (!result)
            {
                return NotFound(new { success = false, message = "找不到該車輛的進出紀錄。" });
            }

            return Ok(new { success = true, message = "支付狀態更新成功並已發送通知。" });
        }
        //---------------------------- 停出付款結束 -----------------------------------

        //------------------------------ 檢測停車開始 ------------------------------------
        [HttpPost("FindMyParking")]
        public async Task<IActionResult> FindMyParking([FromBody] ListenCarDTO dto)
        {
            var result = await _myPayService.FindMyParkingAsync(dto);

            if (!(result as dynamic).success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        // ------------------------ 驗證預約停車的金額是否相符開始 -------------------------------

        [HttpPost("ValidateDay")]
        public async Task<IActionResult> ValidateDayPayment([FromBody] PaymentValidationDayDto dto)
        {
            // 呼叫服務層進行驗證
            var (isValid, message) = await _myPayService.ValidateDayPaymentAsync(dto);

            // 如果驗證失敗，回傳 400 BadRequest
            if (!isValid)
            {
                return BadRequest(new { success = false, message });
            }

            // 驗證成功，回傳 200 OK
            return Ok(new { isValid = true });
        }


        // ------------------------ 驗證預約停車的金額是否相符結束 -------------------------------

        [HttpPost("CreateRes")]
        public async Task<IActionResult> CreateRes([FromBody] PaymentRequestDto dto)
        {
            try
            {
                // 1. 使用 LinePay 服務發送支付請求
                var paymentResponse = await _linePayService.SendPaymentRequest(dto);

                // 2. 根據 CarId 和 LotId 查找現有的進出記錄
                var existingRecord = await _context.EntryExitManagement
                    .OrderByDescending(e => e.EntryexitId)
                    .FirstOrDefaultAsync(e => e.CarId == dto.CarId && e.LotId == dto.LotId);

                if (existingRecord == null)
                {
                    // 3. 如果找不到記錄，返回錯誤訊息
                    return BadRequest(new { success = false, message = "未找到該車輛的進出記錄。" });
                }
                var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                var taiwanTime = TimeZoneInfo.ConvertTime(DateTime.Now, taiwanTimeZone);
                // 4. 更新現有記錄的出場時間和支付金額
                existingRecord.LicensePlateKeyinTime = taiwanTime; // 設定出場時間為當前時間
                existingRecord.Amount = dto.Amount; // 更新支付金額

                // 5. 標記記錄為更新
                _context.EntryExitManagement.Update(existingRecord);

                // 6. 保存變更
                await _context.SaveChangesAsync();

                // 7. 回傳支付結果
                return Ok(paymentResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"發生錯誤：{ex.Message}");

                // 回傳錯誤回應
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = $"處理支付時發生錯誤。,{ex}" });
            }
        }

        //-------------------------------------------------------------------------------------------

        [HttpPost("Confirm")]
        public async Task<PaymentConfirmResponseDto> ConfirmPayment([FromQuery] string transactionId, [FromQuery] string orderId, PaymentConfirmDto dto)
        {

            return await _linePayService.ConfirmPayment(transactionId, orderId, dto);
        }



        [HttpGet("Cancel")]
        public async void CancelTransaction([FromQuery] string transactionId)
        {
            _linePayService.TransactionCancel(transactionId);
        }


    }

}