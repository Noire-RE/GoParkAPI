namespace GoParkAPI.DTO
{
    public class ECpayDTO
    {
        public string? ItemName { get; set; }    // 商品名稱
        public string? PlanName { get; set; } //方案名稱
        public int TotalAmount { get; set; } // 交易金額
        public string ClientBackURL { get; set; }
        public int LotId { get; set; }
        public int CarId { get; set; }
        public string? PlanId { get; set; }
        public string? OrderId { get; set; }
        public DateTime? StartTime { get; set; }
    }

    // 回傳資料 DTO
    public class ECpayCallbackDTO
    {
        public string MerchantTradeNo { get; set; }
        public string TradeNo { get; set; }
        public string RtnCode { get; set; }
        public string PaymentDate { get; set; }
        public string CheckMacValue { get; set; }
    }
}
