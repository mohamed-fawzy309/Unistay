using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class AttendanceSetting
{
    public int ID { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public decimal? ConfidenceThreshold { get; set; }

    public bool? IsEnabled { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
