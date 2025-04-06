using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class ParkingLotImages
{
    public int ImageId { get; set; }

    public int? LotId { get; set; }

    public string? ImgTitle { get; set; }

    public string? ImgPath { get; set; }

    public virtual ParkingLots? Lot { get; set; }
}
