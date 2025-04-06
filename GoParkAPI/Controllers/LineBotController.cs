using GoParkAPI.DTO.Webhook;
using GoParkAPI.Services.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LineBotController : ControllerBase
    {
        //宣告 service
        private readonly LineBotService _lineBotService;
        private readonly ILogger<LineBotController> _logger;


        // constructor
        public LineBotController(LineBotService lineBotService, ILogger<LineBotController> logger)
        {
            _lineBotService = lineBotService;
            _logger = logger;
        }

        //此API會接收Line傳送的 webhook event(故使用POST，依據接收事件調用相應的api
        [HttpPost("Webhook")]
        public IActionResult Webhook(WebhookRequestBodyDto body)
        {
            try
            {
                _logger.LogInformation("收到 webhook 事件");
                _logger.LogInformation("收到 webhook 事件: {Body}", JsonConvert.SerializeObject(body));
                _lineBotService.ReceiveWebhook(body);  // 呼叫 service
                return Ok();
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "處理 webhook 事件時發生錯誤: {ErrorDetails}", ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, "發生錯誤");
            }
        }

        //廣播訊息(推播訊息給官方帳號所有好友)
        [HttpPost("SendMessage/Broadcast")]
        public IActionResult Broadcast(string messageType, object body)
        {
            _lineBotService.BroadcastMessageHandler(messageType, body);
            return Ok();
        }

    }
}
