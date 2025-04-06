using GoParkAPI.DTO.Actions;
using GoParkAPI.DTO.Messages;
using GoParkAPI.DTO.Messages.Request;
using GoParkAPI.Models;
using GoParkAPI.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using static GoParkAPI.Enum.MessageEnum;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LineBindingController : ControllerBase
    {
        private readonly EasyParkContext _context;
        private readonly ILogger<LineBotController> _logger;
        private readonly HttpClient _httpClient;
        private readonly JsonProvider _jsonProvider = new JsonProvider();

        private readonly string channelAccessToken = "ryqtZiA6xa3TwMai/8Xqrgd7u8BRaPuw2fa/XhjG3Ij+contVfz60Uv8yuBXt4XTALlsRe2JUcTluWuSQlOhXkqvmWG27IoO8zsmdtSDa7iPOeKhh+hG5aS1Vcy5DFqQT4uaziHnsQHL8wiAoKbZ5wdB04t89/1O/w1cDnyilFU=";

        public LineBindingController(EasyParkContext context, ILogger<LineBotController> logger, HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
        }


        [HttpPost]
        [Route("bind")]
        public async Task<IActionResult> Bind([FromBody] BindRequest request)
        {
            // 驗證是否具備所需參數
            if (string.IsNullOrEmpty(request.line_user_id) || string.IsNullOrEmpty(request.user_id))
            {
                _logger.LogWarning("缺少必要參數: line_user_id 或 user_id");
                return BadRequest(new { success = false, message = "缺少必要參數" });
            }
            //_logger.LogInformation($"綁定 LINE UserID = {request.line_user_id} 給網站使用者 {request.user_id}");

            // 檢查UserId是否為有效的整數
            if (!int.TryParse(request.user_id, out int userIdInt))
            {
                _logger.LogWarning($"無效的 User ID: {request.user_id}");
                return BadRequest(new { success = false, message = "無效的 User ID" });
            }

            // 檢查是否已經綁定過
            var existBinding = await _context.LineBinding
                .FirstOrDefaultAsync(b => b.UserId == userIdInt || b.LineUserId == request.line_user_id);

            if (existBinding != null)
            {
                return Conflict(new { success = false, message = "此帳號已經綁定過" });
            }

            LineBinding binding = new LineBinding
            {
                UserId = userIdInt,
                LineUserId = request.line_user_id,
            };
            _context.LineBinding.Add(binding);

            try
            {
                await _context.SaveChangesAsync();  //儲存ID對應資料
                // 同時發送 LINE 訊息通知綁定成功                                    
                var lineApiResponse = await SendLineMessage(request.line_user_id);
                _logger.LogInformation($"綁定成功：LINE UserID = {request.line_user_id} 給網站使用者 {request.user_id}");
                return Ok(new { success = true, message = "綁定成功", lineMessageResponse = lineApiResponse });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "資料庫異常");
                return StatusCode(500, new { success = false, message = "伺服器錯誤" });
            }


        }



        // 主動發送訊息給 LINE 用戶
        private async Task<string> SendLineMessage(string lineUserId)
        {
            // 設定 HTTP client 標頭
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channelAccessToken); // 輸入您的 channel access token

            // 構建訊息的內容
            var requestBody = new
            {
                to = lineUserId,
                messages = new List<TemplateMessageDto<ButtonsTemplateDto>>
                {
                    new TemplateMessageDto<ButtonsTemplateDto>
                    {
                        AltText = "請選擇服務項目",
                        Template = new ButtonsTemplateDto
                        {
                            ThumbnailImageUrl = "https://i.imgur.com/jTqLkmN.png",
                            ImageAspectRatio = TemplateImageAspectRatioEnum.Rectangle,
                            ImageSize = TemplateImageSizeEnum.Contain,
                            Title = "會員綁定成功🎉\n立即體驗MyGoParking的服務!🚗",
                            Text = "請選擇服務項目。",
                            Actions = new List<ActionDto>
                            {
                                new ActionDto
                                {
                                    Type = ActionTypeEnum.Postback,
                                    Data = "action=booking_query",
                                    Label = "車位預訂查詢",
                                    DisplayText = "車位預訂查詢"
                                },
                                new ActionDto
                                {
                                    Type = ActionTypeEnum.Postback,
                                    Data = "action=record_query",
                                    Label = "停車紀錄查詢",
                                    DisplayText = "停車紀錄查詢"
                                },
                                new ActionDto
                                {
                                    Type = ActionTypeEnum.Postback,
                                    Data = "action=monthly_rent_query",
                                    Label = "車位月租查詢",
                                    DisplayText = "車位月租查詢"
                                }
                            }
                        }
                    }
                }

            };

            // 序列化訊息為 JSON
            var json = _jsonProvider.Serialize(requestBody);

            // 構建 HTTP 請求
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.line.me/v2/bot/message/push"), // LINE API 端點
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            try
            {
                // 發送 HTTP 請求
                var response = await _httpClient.SendAsync(requestMessage);

                // 檢查回應狀態
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Message sent successfully to LINE user.");
                    return await response.Content.ReadAsStringAsync(); // 回傳訊息內容
                }
                else
                {
                    // 發送失敗，記錄錯誤
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error sending message. Status code: {response.StatusCode}. Response: {errorContent}");
                    return errorContent;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public class BindRequest
        {
            public string line_user_id { get; set; }
            public string user_id { get; set; }
        }


    }
}
