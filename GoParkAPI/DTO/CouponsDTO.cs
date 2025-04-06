namespace GoParkAPI.DTO
{
    public class CouponsDTO
    {
        public int couponId { get; set; }

        public string couponCode { get; set; } = null!;

        public int? discountAmount { get; set; }

        public DateTime validFrom { get; set; }

        public DateTime validUntil { get; set; }

        public bool isUsed { get; set; }
    }
}