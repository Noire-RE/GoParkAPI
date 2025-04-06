namespace GoParkAPI.DTO
{
    public class PaymentValidationDto
    {
        public string PlanId { get; set; }
        public int Amount { get; set; }
        public int LotId { get; set; }
    }

    public class PaymentValidationDayDto
    {
        public int carId { get; set; }
        public int LotId { get; set; }
        public int Amount { get; set; }
        public int? couponsId { get; set; }

    }
}

