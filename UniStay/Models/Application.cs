using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class Application
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int DormitoryCityID { get; set; }

    public string AcademicYear { get; set; } = null!;

    public string StudentType { get; set; } = null!;

    public string HousingType { get; set; } = null!;

    public bool? MealSubscription { get; set; }

    public bool? HasSpecialNeeds { get; set; }

    public string? SpecialNeedsDescription { get; set; }

    public string Status { get; set; } = null!;

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }

    public string? AdminNotes { get; set; }

    public decimal? CoordinationScore { get; set; }

    public int? CoordinationRank { get; set; }

    public string ServerVerificationStatus { get; set; } = null!;

    public DateTime? ServerVerificationAt { get; set; }

    public int? ServerVerificationBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public int? LastUpdatedBy { get; set; }

    public virtual Allocation? Allocation { get; set; }

    public virtual ICollection<CoordinationResult> CoordinationResults { get; set; } = new List<CoordinationResult>();

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual SystemUser? LastUpdatedByNavigation { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual SystemUser? ReviewedByNavigation { get; set; }

    public virtual SystemUser? ServerVerificationByNavigation { get; set; }



    public virtual Student Student { get; set; } = null!;
}
