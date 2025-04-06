namespace GoParkAPI.DTO
{
    public class LotsInfoDTO
    {
        public int LotId { get; set; }

        public string? LotName { get; set; }

        public List<string> LotImages { get; set; } // 停車場圖片 URL 列表

        public string? Location { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public int SmallCarSpace { get; set; }

        public int MonRentalSpace { get; set; }

        public int EtcSpace { get; set; }

        public int MotherSpace { get; set; }

        public string? RateRules { get; set; }

        public int WeekdayRate { get; set; }

        public int HolidayRate { get; set; }

        public int MonRentalRate { get; set; }

        public int ResDeposit { get; set; }

        public string Tel { get; set; } = null!;

        public int ValidSpace { get; set; }

    }
}
