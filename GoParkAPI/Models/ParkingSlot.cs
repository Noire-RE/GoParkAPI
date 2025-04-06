using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class ParkingSlot
{
    public int SlotId { get; set; }

    public int LotId { get; set; }

    public int? SlotNum { get; set; }

    public bool IsRented { get; set; }

    public virtual ParkingLots Lot { get; set; } = null!;
}
