using GoParkAPI.Enum;

namespace GoParkAPI.DTO.Messages
{
    public class StickerMessageDto:BaseMessageDto
    {
        public StickerMessageDto()
        {
            Type = MessageEnum.MessageTypeEnum.Sticker;
        }
        public string PackageId { get; set; }
        public string StickerId { get; set; }
    }
}
