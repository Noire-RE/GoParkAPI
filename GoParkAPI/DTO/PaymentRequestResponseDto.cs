namespace GoParkAPI.DTO
{
    public class PaymentResponseDto
    {
        public string ReturnCode { get; set; } // API 回應碼
        public string ReturnMessage { get; set; } // API 回應訊息
        public ResponseInfoDto Info { get; set; } // 交易詳細資訊

        // 新增交易狀態與交易 ID
        public string TransactionStatus { get; set; } // "SUCCESS", "FAILURE" 等狀態
        public string TransactionId { get; set; } // 用來追蹤的交易 ID
    }

    public class ResponseInfoDto
    {
        public ResponsePaymentUrlDto PaymentUrl { get; set; }
        public long TransactionId { get; set; }
        public string PaymentAccessToken { get; set; }
        public string OrderId { get; set; }
    }

    public class ResponsePaymentUrlDto
    {
        public string Web { get; set; }
        public string App { get; set; }
    }
}
