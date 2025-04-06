using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Revenue
{
    public int RevenueId { get; set; }

    public DateOnly Date { get; set; }

    public int TotalAmount { get; set; }

    public int RentalIncome { get; set; }

    public int ReservationIncome { get; set; }

    public DateTime CreatedTime { get; set; }
}
