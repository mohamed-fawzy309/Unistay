using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class SpecialCase
{
    public int ID { get; set; }

    public int ApplicationID { get; set; }

    public int StudentID { get; set; }

    public string CaseType { get; set; } = null!;

    public string? Description { get; set; }

    public string? SupportingDocuments { get; set; }

    public string? Status { get; set; }

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNotes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Application Application { get; set; } = null!;

    public virtual SystemUser? ReviewedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
