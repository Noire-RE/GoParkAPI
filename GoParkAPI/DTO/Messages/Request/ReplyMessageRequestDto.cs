namespace GoParkAPI.DTO.Messages.Request
{
    public class ReplyMessageRequestDto<T>   //針對自動回覆用戶訊息(當用戶發送訊息請求資料會用到)
    {
        public string ReplyToken { get; set; }
        public List<T> Messages { get; set; }
        public bool? NotificationDisabled { get; set; }
    }
}
