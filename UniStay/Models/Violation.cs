using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class Violation
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int DormitoryCityID { get; set; }

    public string ViolationType { get; set; } = null!;

    public string? Description { get; set; }

    public string Severity { get; set; } = null!;

    public decimal? FineAmount { get; set; }

    public decimal? FinePaid { get; set; }

    public string Status { get; set; } = null!;

    public bool? IsOnBlacklist { get; set; }

    public int? RecordedBy { get; set; }

    public DateTime? RecordedAt { get; set; }

    public int? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual SystemUser? RecordedByNavigation { get; set; }

    public virtual SystemUser? ResolvedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
