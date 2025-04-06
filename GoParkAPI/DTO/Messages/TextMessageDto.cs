using GoParkAPI.Enum;

namespace GoParkAPI.DTO.Messages
{
    public class TextMessageDto:BaseMessageDto
    {
        //------屬性-----
        public TextMessageDto()
        {
            Type = MessageEnum.MessageTypeEnum.Text;
        }
        public string Text { get; set; }
        public List<TextMessageEmojiDto>? Emojis { get; set; }  //表情符號，可有可沒有


        //----------------
        //表情符號
        public class TextMessageEmojiDto
        {
            public int Index { get; set; }
            public string ProductId { get; set; }
            public string EmojiId { get; set; }
        }
    }
}
