using GoParkAPI.Enum;

namespace GoParkAPI.DTO.Messages
{
    public class ImageMessageDto:BaseMessageDto
    {
        public ImageMessageDto()
        {
            Type = MessageEnum.MessageTypeEnum.Image;
        }
        public string OriginalContentUrl { get; set; }
        public string PreviewImageUrl { get; set; }
    }
}
