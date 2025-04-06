namespace GoParkAPI.Enum
{
    public class WebhookEventTypeEnum
    {
        //因為 C# Enum不能直接使用string做值，所以才建立這個class代替其功能。
        public const string Message = "message"; //用戶發送訊息觸發事件
        public const string Unsend = "unsend";   //用戶收回訊息..
        public const string Follow = "follow"; //用戶將帳號加為好友
        public const string UnFollow = "unfollow"; //用戶封鎖該官方
        public const string Join = "join"; //用戶將官方帳號加到群組觸發事件
        public const string Leave = "leave"; //用戶將關方帳號踢出群組

        //-------自己新增的
        public const string Postback = "postback"; //當用戶點選表單內容後會返回的事件資料類型
    }
}
