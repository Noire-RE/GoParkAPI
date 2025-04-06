using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class EntryExitManagement
{
    public int EntryexitId { get; set; }

    public int LotId { get; set; }

    public int CarId { get; set; }

    public string Parktype { get; set; } = null!;

    public string? LicensePlatePhoto { get; set; }

    public DateTime? EntryTime { get; set; }

    public DateTime? LicensePlateKeyinTime { get; set; }

    public int? Amount { get; set; }

    public DateTime? PaymentTime { get; set; }

    public bool PaymentStatus { get; set; }

    public DateTime? ValidTime { get; set; }

    public DateTime? ExitTime { get; set; }

    public bool IsFinish { get; set; }

    public virtual Car Car { get; set; } = null!;

    public virtual ParkingLots Lot { get; set; } = null!;
}
