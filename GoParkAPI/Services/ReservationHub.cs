using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using WebPush;

namespace GoParkAPI.Services
{
    public class ReservationHub : Hub
    {
        private readonly PushNotificationService _pushNotificationService;

        public ReservationHub(PushNotificationService pushNotificationService)
        {
            _pushNotificationService = pushNotificationService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            await Clients.All.SendAsync("UserConnected", userId);
            await base.OnConnectedAsync();
        }

        public async Task SendOverdueReminderNotification(int resId, string userId)
        {
            var notificationSent = await _pushNotificationService.CheckAndSendOverdueReminder(resId);
            await Clients.User(userId).SendAsync(
                "ReceiveNotification",
                "預約提醒",
                "您的預約將在30分鐘後超時，請在安全前提下盡快入場，逾時車位不保留。"
            );
        }

        public async Task SendAlreadyOverdueReminderNotification(int resId, string userId)
        {
            var notificationSent = await _pushNotificationService.CheckAlreadyOverdueRemider(resId);
            await Clients.User(userId).SendAsync(
                "ReceiveNotification",
                "預約超時提醒",
                "你的預約已超時!!"
            );
        }
    }
}
