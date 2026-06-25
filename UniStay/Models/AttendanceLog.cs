using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class AttendanceLog
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int SessionID { get; set; }

    public DateTime? RecognizedAt { get; set; }

    public decimal? Confidence { get; set; }

    public virtual AttendanceSession AttendanceSession { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
