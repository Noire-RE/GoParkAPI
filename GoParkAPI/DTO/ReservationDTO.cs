namespace GoParkAPI.DTO
{
    public class ReservationDTO
    {
        public int resId { get; set; }

        public DateTime? resTime { get; set; }

        public string lotName { get; set; } = null!;

        public string? district { get; set; } = null!;

        public string? location { get; set; } = null!;

        public string licensePlate { get; set; } = null!;

        public DateTime startTime { get; set; } //預約進場時間

        public DateTime validUntil { get; set; }  //預約進場時間+時限 //用來判斷若現在訂單還沒完成，是否逾期，若未逾期則可取消訂單

        public bool PaymentStatus { get; set; }
       
        public bool isCanceled { get; set; }

        public bool isOverdue { get; set; }

        public bool isFinish { get; set; }

        public decimal? latitude { get; set; }  //緯度

        public decimal? longitude { get; set; }  //經度

        public int? lotId { get; set; }  // 為了要在預定紀錄導入到預定畫面用需要lotID


    }




}