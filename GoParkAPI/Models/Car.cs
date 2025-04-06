using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Car
{
    public int CarId { get; set; }

    public int UserId { get; set; }

    public string LicensePlate { get; set; } = null!;

    public DateTime? RegisterDate { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<DealRecord> DealRecord { get; set; } = new List<DealRecord>();

    public virtual ICollection<EntryExitManagement> EntryExitManagement { get; set; } = new List<EntryExitManagement>();

    public virtual ICollection<MonApplyList> MonApplyList { get; set; } = new List<MonApplyList>();

    public virtual ICollection<MonthlyRental> MonthlyRental { get; set; } = new List<MonthlyRental>();

    public virtual ICollection<Reservation> Reservation { get; set; } = new List<Reservation>();

    public virtual Customer User { get; set; } = null!;
}
