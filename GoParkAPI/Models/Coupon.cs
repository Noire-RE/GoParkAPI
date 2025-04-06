using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Coupon
{
    public int CouponId { get; set; }

    public string CouponCode { get; set; } = null!;

    public int? DiscountAmount { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidUntil { get; set; }

    public bool IsUsed { get; set; }

    public int? UserId { get; set; }

    public virtual Customer? User { get; set; }
}
