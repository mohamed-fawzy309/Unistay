using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class StudentValidationLog
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public string ValidationType { get; set; } = null!;

    public bool IsValid { get; set; }

    public string? IssueSeverity { get; set; }

    public string? IssueDescription { get; set; }

    public bool? IsResolved { get; set; }

    public int? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual SystemUser? ResolvedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
