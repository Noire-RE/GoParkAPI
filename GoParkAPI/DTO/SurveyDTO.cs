using GoParkAPI.Models;

namespace GoParkAPI.DTO
{
    public class SurveyDTO
    {
        public int UserId { get; set; }

        public string Question { get; set; } = null!;
    }
}
