using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class DealRecord
{
    public int DealId { get; set; }

    public int CarId { get; set; }

    public int Amount { get; set; }

    public DateTime PaymentTime { get; set; }

    public string ParkType { get; set; } = null!;

    public virtual Car Car { get; set; } = null!;
}
