using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Survey
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Question { get; set; } = null!;

    public string? ReplyMessage { get; set; }

    public bool IsReplied { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? RepliedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual Customer User { get; set; } = null!;
}
