namespace GoParkAPI.DTO.Messages.Request
{
    public class BroadcastMessageRequestDto<T> //針對廣播訊息，當某事件發生，可以廣播訊息給特定或全部用戶(如到期通知)
    {
        public List<T> Messages { get; set; }
        public bool? NotificationDisabled { get; set; }
    }
}
