using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class StudentDownloadLog
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public string FormType { get; set; } = null!;

    public DateTime? DownloadedAt { get; set; }

    public int? DownloadedBy { get; set; }

    public virtual SystemUser? DownloadedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
