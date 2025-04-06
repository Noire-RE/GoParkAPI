namespace GoParkAPI.DTO
{
    public class CustomerDTO
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Salt { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? LicensePlate { get; set; }
        public bool IsBlack { get; set; }

    }

    public class EditDTO
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? LicensePlate { get; set; }
    }


    public class CouponDTO
    {
        public int CouponId { get; set; }
        public string CouponCode { get; set; } = null!;
        public int? DiscountAmount { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime ValidUntil { get; set; }

        public bool IsUsed { get; set; }

        public int? UserId { get; set; }
    }
}