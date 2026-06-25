using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class AttendanceSession
{
    public int ID { get; set; }

    public string SessionName { get; set; } = null!;

    public DateTime? StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
}
