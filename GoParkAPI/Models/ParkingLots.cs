using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class ParkingLots
{
    public int LotId { get; set; }

    public string? District { get; set; }

    public string? Type { get; set; }

    public string? LotName { get; set; }

    public string? Location { get; set; }

    public int MonRentalSpace { get; set; }

    public int SmallCarSpace { get; set; }

    public int EtcSpace { get; set; }

    public int MotoSpace { get; set; }

    public int MotherSpace { get; set; }

    public string? RateRules { get; set; }

    public int WeekdayRate { get; set; }

    public int HolidayRate { get; set; }

    public int ResDeposit { get; set; }

    public int MonRentalRate { get; set; }

    public string? OpendoorTime { get; set; }

    public string Tel { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int ValidSpace { get; set; }

    //public int ResOverdueValidTimeSet { get; set; }

    public virtual ICollection<EntryExitManagement> EntryExitManagement { get; set; } = new List<EntryExitManagement>();

    public virtual ICollection<MonApplyList> MonApplyList { get; set; } = new List<MonApplyList>();

    public virtual ICollection<MonthlyRental> MonthlyRental { get; set; } = new List<MonthlyRental>();

    public virtual ICollection<ParkingLotImages> ParkingLotImages { get; set; } = new List<ParkingLotImages>();

    public virtual ICollection<ParkingSlot> ParkingSlot { get; set; } = new List<ParkingSlot>();

    public virtual ICollection<Reservation> Reservation { get; set; } = new List<Reservation>();
}
