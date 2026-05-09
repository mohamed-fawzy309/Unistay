using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class IDCard
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public string CardNumber { get; set; } = null!;

    public string? Barcode { get; set; }

    public string? QRCode { get; set; }

    public bool? IsPrinted { get; set; }

    public bool? IsActive { get; set; }

    public byte? ReprintCount { get; set; }

    public DateTime? PrintedAt { get; set; }

    public int? PrintedBy { get; set; }

    public virtual SystemUser? PrintedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
