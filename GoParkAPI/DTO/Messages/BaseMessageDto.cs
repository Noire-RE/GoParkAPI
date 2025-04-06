using GoParkAPI.DTO.Actions;

namespace GoParkAPI.DTO.Messages
{
    public class BaseMessageDto
    {
        public string Type { get; set; }

        // Quick reply
        public QuickReplyItemDto QuickReply { get; set; }


        //-----------
        public class QuickReplyItemDto
        {
            public List<QuickReplyButtonDto> Items { get; set; }
        }

        public class QuickReplyButtonDto
        {
            public string Type { get; set; } = "action";
            public string? ImageUrl { get; set; }
            public ActionDto Action { get; set; }
        }
    }
}
