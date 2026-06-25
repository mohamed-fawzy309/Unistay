using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class AttendanceApiLog
{
    public int ID { get; set; }

    public int? StudentID { get; set; }

    public string? Status { get; set; }

    public string? Message { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Student? Student { get; set; }
}
