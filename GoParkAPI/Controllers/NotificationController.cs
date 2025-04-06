using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace GoParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly PushNotificationService _pushNotificationService;
        private readonly EasyParkContext _context;
        private readonly IHubContext<ReservationHub> _hubContext;
        public NotificationController(PushNotificationService pushNotificationService, EasyParkContext easyPark, IHubContext<ReservationHub> hubContext)
        {
            _pushNotificationService = pushNotificationService;
            _context = easyPark;
            _hubContext = hubContext;
        }

        [HttpPost("subscribe")]
        public IActionResult Subscribe([FromBody] PushSubscription subscription)
        {
            _pushNotificationService.AddSubscription(subscription);
            return Ok();
        }

        //TEST
        [HttpPost("send")]
        public async Task<IActionResult?> SendNotification([FromBody] NotificationRequestDTO requestDTO)
        {
            await _pushNotificationService.SendNotificationAsync(requestDTO.Title, requestDTO.Message);
            return Ok(new { title = requestDTO.Title, body = requestDTO.Message, MessageResult = "通知發送成功" });
        }

        //[HttpGet("CheckAndSendOverdueReminder")]
        //public async Task<IActionResult> CheckAndSendOverdueReminder(int userId)
        //{
        //    var now = DateTime.Now;
        //    var minutesLater = now.AddMinutes(30);

        //    // 檢查用戶車輛是否存在
        //    var userCars = await _context.Car.Where(x => x.UserId == userId).ToListAsync();
        //    if (!userCars.Any())
        //    {
        //        return NoContent();
        //    }

        //    // 用來儲存所有通知的清單
        //    var notifications = new List<object>();

        //    // 1. 檢查並處理即將超時的預約
        //    var reservationsToExpire = await _context.Reservation
        //        .Where(r => !r.IsFinish && !r.NotificationStatus && r.PaymentStatus && r.StartTime <= minutesLater && r.StartTime > now)
        //        .ToListAsync();

        //    foreach (var res in reservationsToExpire)
        //    {
        //        string title = "預約提醒";
        //        string message = "您的預約將在30分鐘後超時，請在安全前提下盡快入場，逾時車位不保留。";

        //        // 發送通知
        //        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", title, message);
        //        notifications.Add(new { title, message });

        //        // 更新通知狀態
        //        res.NotificationStatus = true;
        //    }

        //    // 立即保存即將超時的更改
        //    await _context.SaveChangesAsync();

        //    // 2. 檢查並處理已超時的預約
        //    var overdueReservations = await _context.Reservation
        //        .Where(r => !r.IsFinish && r.NotificationStatus && r.ValidUntil <= now)
        //        .ToListAsync();

        //    foreach (var res in overdueReservations)
        //    {
        //        string title = "超時";
        //        string message = "您的預約已超時";

        //        // 標記預約已超時
        //        res.IsOverdue = true;
        //        res.IsFinish = true;

        //        // 更新黑名單狀態
        //        var user = await _context.Customer.FindAsync(userId);
        //        if (user != null)
        //        {
        //            user.BlackCount += 1;
        //            if (user.BlackCount >= 3)
        //            {
        //                user.IsBlack = true;
        //            }
        //        }

        //        // 發送通知
        //        await _pushNotificationService.SendNotificationAsync(title, message);
        //        notifications.Add(new { title, message });
        //    }

        //    // 保存已超時的更改
        //    await _context.SaveChangesAsync();

        //    // 回傳所有通知資訊
        //    return notifications.Any() ? Ok(notifications) : NoContent();
        //}
    }
}
