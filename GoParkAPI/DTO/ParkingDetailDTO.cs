namespace GoParkAPI.DTO
{
    public class ParkingDetailDTO
    {
        public int entryexitId { get; set; }

        public string lotName { get; set; } = null!;

        public string? district { get; set; } = null!;

        public string? location { get; set; } = null!;

        public decimal? latitude { get; set; }  //緯度

        public decimal? longitude { get; set; }  //經度

        public string licensePlate { get; set; } = null!;

        public DateTime entryTime { get; set; }

        public DateTime? exitTime { get; set; }

        public int? amount { get; set; }

        public string? formatTime
        {
            get
            {
                if (exitTime.HasValue)
                {
                    TimeSpan duration = (TimeSpan)(exitTime - entryTime);
                    return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                }
                return null;  // 或者返回 "N/A" 或其他預設值
            }
        }

    }
}