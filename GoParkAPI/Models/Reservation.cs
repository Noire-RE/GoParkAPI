using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Reservation
{
    public int ResId { get; set; }

    public int CarId { get; set; }

    public int LotId { get; set; }

    public DateTime? ResTime { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime? StartTime { get; set; }

    public bool PaymentStatus { get; set; }

    public bool IsCanceled { get; set; }

    public bool IsOverdue { get; set; }

    public bool IsRefoundDeposit { get; set; }

    public bool NotificationStatus { get; set; }

    public bool IsFinish { get; set; }

    public string? TransactionId { get; set; }

    public virtual Car Car { get; set; } = null!;

    public virtual ParkingLots Lot { get; set; } = null!;
}
