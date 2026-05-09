using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class CoordinationResult
{
    public int ID { get; set; }

    public int ApplicationID { get; set; }

    public int StudentID { get; set; }

    public int DormitoryCityID { get; set; }

    public string? AcademicYear { get; set; }

    public decimal? DistanceScore { get; set; }

    public decimal? GradeScore { get; set; }

    public decimal? AgeScore { get; set; }

    public decimal? SpecialBonus { get; set; }

    public decimal? TotalScore { get; set; }

    public int? Rank { get; set; }

    public string? Status { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int? ProcessedBy { get; set; }

    public virtual Application Application { get; set; } = null!;

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual SystemUser? ProcessedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
