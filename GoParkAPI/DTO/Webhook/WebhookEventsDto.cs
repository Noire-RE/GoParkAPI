namespace GoParkAPI.DTO.Webhook
{
    public class WebhookEventDto
    {
        // -------- 以下為 common property --------
        public string Type { get; set; } // 事件類型(Event Type)，如"message | postback

        public string Mode { get; set; } // Channel state : active | standby

        public long Timestamp { get; set; } // 事件發生時間 

        public SourceDto Source { get; set; } // 事件來源 : user | group chat | multi-person chat

        public string WebhookEventId { get; set; }

        public DeliverycontextDto DeliveryContext { get; set; } // 是否為重新傳送之事件 DeliveryContext.IsRedelivery : true | false

        public string? ReplyToken { get; set; } //回覆此事件所使用的token

        public MessageEventDto? Message { get; set; } //收到訊息的事件，當收到 text、sticker、image、file、video、audio、location會有此屬性

        public PostbackEventDto? Postback { get; set; }

        public UnsendEventDto? Unsend { get; set; } //使用者“收回”訊息事件

        public FollowEventDto? Follow { get; set; } //使用者加入好友或解除封鎖
    }

    //------------------------------
    public class SourceDto
    {
        public string Type { get; set; }
        public string? UserId { get; set; }
        public string? GroupId { get; set; }
        public string? RoomId { get; set; }
    }

    public class DeliverycontextDto
    {
        public bool IsRedelivery { get; set; }

    }

    //-----------Postback Event :當有觸發action的postback
    public class PostbackEventDto
    {
        public string Data { get; set; }
        public ParamsDto? Params { get; set; }
    }

    public class ParamsDto
    {
        public string? Date { get; set; }
    }

    //-------------Message Event 當傳送訊息會有此屬性-----------------
    public class MessageEventDto
    {
        public string Id { get; set; }
        public string Type { get; set; }

        //1. Text Message Event :如果是發送文字訊息
        public string? Text { get; set; }

        public List<TextMessageEventEmojiDto>? Emojis { get; set; } //如果訊息有表情符號

        public TextMessageEventMentionDto? MentionDto { get; set; } //如果訊息有提及誰(標記)

        //2. Image Message Event 當發送照片

        public ContentProviderDto? ContentProvider { get; set; }
        public ImageMessageEventImageSetDto? ImageSet { get; set; }

        //3.4. Video/Audio Message Event 當發送影片或音檔
        public int? Duration { get; set; } //影片 or 音檔時長

        //5. File Message Event 當傳送檔案
        public string? FileName { get; set; }
        public int? FileSize { get; set; }

        //6. Location Message Event 當發送位置
        public string? Title { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        //7. Sticker Message Event
        public string? StickerId { get; set; } // 貼圖 ID
        public string? PackageId { get; set; } // 貼圖包 ID
        public string? StickerResourceType { get; set; } // 貼圖資源類型，例如 "MESSAGE"
        public List<string>? Keywords { get; set; } // 貼圖關鍵詞清單
    }

    public class TextMessageEventEmojiDto
    {
        public int Index { get; set; }
        public int Length { get; set; }
        public string ProductId { get; set; }
        public string EmojiId { get; set; }
    }

    // 1. Text
    public class TextMessageEventMentionDto
    {
        public List<TextMessageEventMentioneeDto> Mentionees { get; set; }
    }

    public class TextMessageEventMentioneeDto
    {
        public int Index { get; set; }
        public int Length { get; set; }
        public string UserId { get; set; }
    }

    //2. Image 
    public class ContentProviderDto
    {
        public string Type { get; set; }
        public string? OriginalContentUrl { get; set; }
        public string? PreviewImageUrl { get; set; }
    }

    public class ImageMessageEventImageSetDto
    {
        public string Id { get; set; }
        public string Index { get; set; }
        public string Total { get; set; }
    }

    public class UnsendEventDto
    {
        public string messageId { get; set; }
    }

    public class FollowEventDto  //新增
    {
        public bool IsUnblocked { get; set; }
    }


}
