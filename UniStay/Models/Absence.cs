using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class Absence
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int DormitoryCityID { get; set; }

    public DateOnly AbsenceDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string AbsenceType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string RequestedBy { get; set; } = null!;

    public string? GuardianName { get; set; }

    public string? GuardianRelation { get; set; }

    public string? GuardianPhone { get; set; }

    public string? Reason { get; set; }

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual SystemUser? ReviewedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
