namespace UniStay.ViewModels.Application;

public class ReviewApplicationViewModel
{
    // Application info
    public int ApplicationID { get; set; }
    public string AcademicYear { get; set; } = null!;
    public string StudentType { get; set; } = null!;
    public string HousingType { get; set; } = null!;
    public bool? MealSubscription { get; set; }
    public bool? HasSpecialNeeds { get; set; }
    public string? SpecialNeedsDescription { get; set; }
    public string Status { get; set; } = null!;
    public string? ServerVerificationStatus { get; set; }
    public decimal? CoordinationScore { get; set; }
    public int? CoordinationRank { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public string? CurrentRejectionReason { get; set; }
    public string? CurrentAdminNotes { get; set; }

    // DormitoryCity
    public string DormitoryCityName { get; set; } = null!;
    public string DormitoryCityType { get; set; } = null!;

    // Student info
    public int StudentID { get; set; }
    public string StudentName { get; set; } = null!;
    public string? StudentNationalID { get; set; }
    public string? StudentCode { get; set; }
    public string Gender { get; set; } = null!;
    public DateOnly BirthDate { get; set; }
    public string Religion { get; set; } = null!;
    public string Nationality { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public byte? StudentAcademicYear { get; set; }
    public string? Governorate { get; set; }
    public string? Markaz { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public decimal? DistanceFromUniv { get; set; }
    public decimal? GradePercentage { get; set; }
    public string? GradeText { get; set; }
    public string? Photo { get; set; }
    public bool? HasDisability { get; set; }
    public bool? IsOrphan { get; set; }
    public bool? IsLowIncome { get; set; }
    public bool? HasFamilyAbroad { get; set; }
    public bool? HasMedicalCondition { get; set; }
    public string? MedicalDescription { get; set; }
    public bool? IsForeign { get; set; }

    // Review action
    public string? ReviewAction { get; set; }
    public string? RejectionReason { get; set; }
    public string? AdminNotes { get; set; }
}
