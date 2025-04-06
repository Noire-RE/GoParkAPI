using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Transactions
{
    public int TranId { get; set; }

    public int OrdId { get; set; }

    public int? CouponId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public decimal PaymentAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public bool PaymentStatus { get; set; }

    public DateTime? PaymentTime { get; set; }

    public DateTime CreatedTime { get; set; }

    public virtual Coupon? Coupon { get; set; }

    public virtual Orders Ord { get; set; } = null!;
}
