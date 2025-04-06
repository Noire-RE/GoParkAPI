using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class LineBinding
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string LineUserId { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Customer User { get; set; } = null!;
}
