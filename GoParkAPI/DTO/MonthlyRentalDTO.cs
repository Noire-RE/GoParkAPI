namespace GoParkAPI.DTO
{
    public class MonthlyRentalDTO
    {
        public int renId { get; set; }

        public string licensePlate { get; set; } = null!;

        public int lotId { get; set; }
        public string lotName { get; set; } = null!;

        public decimal? latitude { get; set; }  //緯度

        public decimal? longitude { get; set; }  //經度

        public string? location { get; set; } = null!;

        public string? district { get; set; } = null!;

        public DateTime startDate { get; set; }

        public DateTime endDate { get; set; }

        public int amount { get; set; }

        public int monRentalRate { get; set; }
    }
}