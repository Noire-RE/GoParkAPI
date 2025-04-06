using GoParkAPI.DTO.Actions;
using static GoParkAPI.Enum.MessageEnum;

namespace GoParkAPI.DTO.Messages
{
    public class TemplateMessageDto<T> : BaseMessageDto
    {
        public TemplateMessageDto()
        {
            Type = MessageTypeEnum.Template;
        }
        public string AltText { get; set; }
        public T Template { get; set; }
    }

    //1.button template (選單)
    public class ButtonsTemplateDto
    {
        public string Type { get; set; } = TemplateTypeEnum.Buttons;
        public string Text { get; set; }
        public List<ActionDto>? Actions { get; set; }

        public string? ThumbnailImageUrl { get; set; }
        public string? ImageAspectRatio { get; set; }

        public string? ImageSize { get; set; }
        public string? ImageBackgroundColor { get; set; }
        public string? Title { get; set; }
        public string? DefaultAction { get; set; }
    }

    //2.Confirm template (確認按鈕)
    public class ConfirmTemplateDto
    {
        public string Type { get; set; } = TemplateTypeEnum.Confirm;
        public string Text { get; set; }
        public List<ActionDto>? Actions { get; set; }
    }

    //3. CarouselTemplate (輪播)
    public class CarouselTemplateDto
    {
        public string Type { get; set; } = TemplateTypeEnum.Carousel;
        public List<CarouselColumnObjectDto> Columns { get; set; }

        public string? ImageAspectRatio { get; set; }
        public string? ImageSize { get; set; }
    }
    //輪播的每一個區塊(最多10個)
    public class CarouselColumnObjectDto
    {
        public string Text { get; set; }
        public List<ActionDto> Actions { get; set; }

        public string? ThumbnailImageUrl { get; set; }
        public string? ImageBackgroundColor { get; set; }
        public string? Title { get; set; }
        public ActionDto? DefaultAction { get; set; }
    }
}
