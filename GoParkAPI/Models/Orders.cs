using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Orders
{
    public int OrdId { get; set; }

    public string? OrdType { get; set; }

    public int ReferenceId { get; set; }

    public decimal Amount { get; set; }

    public bool PaymentStatus { get; set; }

    public DateTime? PaymentTime { get; set; }

    public DateTime CreatedTime { get; set; }

    public virtual ICollection<Transactions> Transactions { get; set; } = new List<Transactions>();
}
