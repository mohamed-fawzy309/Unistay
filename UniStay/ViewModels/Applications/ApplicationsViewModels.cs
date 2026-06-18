using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Applications;

public class ApplicationsIndexViewModel
{
    public List<ApplicationRowViewModel> Applications { get; set; } = new();
    public ApplicationsFilterViewModel Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public int UnderReviewCount { get; set; }
}

public class ApplicationsFilterViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? StudentType { get; set; }
    public int? CityID { get; set; }
    public string? Faculty { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
}

public class ApplicationRowViewModel
{
    public int ID { get; set; }
    public string StudentName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? Faculty { get; set; }
    public string? CityName { get; set; }
    public string StudentType { get; set; } = "";
    public string HousingType { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusDisplay { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? ServerVerificationStatus { get; set; }
    public decimal? CoordinationScore { get; set; }
    public int? CoordinationRank { get; set; }
    public string? ReviewedByName { get; set; }
    public int DocumentCount { get; set; }
    public int VerifiedDocCount { get; set; }
}

public class ApplicationDetailViewModel
{
    public int ID { get; set; }
    public string Status { get; set; } = "";
    public string StudentType { get; set; } = "";
    public string HousingType { get; set; } = "";
    public string AcademicYear { get; set; } = "";
    public bool? MealSubscription { get; set; }
    public bool? HasSpecialNeeds { get; set; }
    public string? SpecialNeedsDescription { get; set; }
    public string? RejectionReason { get; set; }
    public string? AdminNotes { get; set; }
    public decimal? CoordinationScore { get; set; }
    public int? CoordinationRank { get; set; }
    public string ServerVerificationStatus { get; set; } = "";
    public DateTime? ServerVerificationAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }

    public StudentInfoViewModel Student { get; set; } = new();
    public CityInfoViewModel DormitoryCity { get; set; } = new();
    public ReviewInfoViewModel? ReviewedBy { get; set; }
    public AllocationInfoViewModel? Allocation { get; set; }
    public List<DocumentInfoViewModel> Documents { get; set; } = new();
    public List<GuardianInfoViewModel> Guardians { get; set; } = new();
}

public class StudentInfoViewModel
{
    public int ID { get; set; }
    public string FullName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? StudentCode { get; set; }
    public string Gender { get; set; } = "";
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public decimal? DistanceFromUniv { get; set; }
    public decimal? GradePercentage { get; set; }
    public bool? HasDisability { get; set; }
    public bool? IsOrphan { get; set; }
    public bool? IsLowIncome { get; set; }
    public bool? HasFamilyAbroad { get; set; }
    public bool? HasMedicalCondition { get; set; }
    public bool? IsForeign { get; set; }
}

public class CityInfoViewModel
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
}

public class ReviewInfoViewModel
{
    public string Name { get; set; } = "";
    public DateTime? ReviewedAt { get; set; }
}

public class AllocationInfoViewModel
{
    public int? ID { get; set; }
    public string? BuildingName { get; set; }
    public string? RoomNumber { get; set; }
    public byte? BedNumber { get; set; }
    public string? Status { get; set; }
}

public class DocumentInfoViewModel
{
    public int ID { get; set; }
    public string DocumentType { get; set; } = "";
    public string? FileName { get; set; }
    public bool? IsVerified { get; set; }
    public DateTime? UploadedAt { get; set; }
}

public class GuardianInfoViewModel
{
    public string FullName { get; set; } = "";
    public string? GuardianType { get; set; }
    public string? Phone { get; set; }
    public string? Job { get; set; }
}

public class ReviewDecisionViewModel
{
    [Required(ErrorMessage = "يرجى اختيار القرار")]
    public string Decision { get; set; } = null!;

    public string? RejectionReason { get; set; }
    public string? AdminNotes { get; set; }
}

public class ReturnForCorrectionViewModel
{
    [Required(ErrorMessage = "ملاحظات التصحيح مطلوبة")]
    public string CorrectionNotes { get; set; } = null!;
}

public class ApplicationReportViewModel
{
    public List<ApplicationRowViewModel> Applications { get; set; } = new();
    public string ReportTitle { get; set; } = "";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public int TotalCount { get; set; }
}

public class CityLookup
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
}
