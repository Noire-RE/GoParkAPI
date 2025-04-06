namespace GoParkAPI.DTO
{
    public class UpdatePaymentStatusDTO
    {
        public string OrderId { get; set; } // 從前端傳來的訂單 ID
        //public int? UserId {  get; set; }

    }
    public class UpdateEntryExitPaymenDTO
    {
        public int? MycouponId { get; set; }
        public int MycarId { get; set; }
        public int Myamount { get; set; }
        
    }
    
}
