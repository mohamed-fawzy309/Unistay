using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Coordination
{
    public class CoordinationRulesViewModel
    {
        public int DormitoryCityID { get; set; }
        public string CityName { get; set; } = null!;
        public List<CoordinationRuleRowViewModel> Rules { get; set; } = new();
        public CreateCoordinationRuleViewModel NewRule { get; set; } = new();
    }

    public class CoordinationRuleRowViewModel
    {
        public int ID { get; set; }
        public string RuleName { get; set; } = null!;
        public string RuleType { get; set; } = null!;
        public byte Priority { get; set; }
        public decimal Weight { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CreateCoordinationRuleViewModel
    {
        [Required(ErrorMessage = "اسم القاعدة مطلوب")]
        [StringLength(200)]
        public string RuleName { get; set; } = null!;

        [Required(ErrorMessage = "نوع القاعدة مطلوب")]
        public string RuleType { get; set; } = null!;

        [Range(1, 255)]
        public byte Priority { get; set; } = 1;

        [Range(0, 100)]
        public decimal Weight { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }

    public class CoordinationPreviewViewModel
    {
        public int DormitoryCityID { get; set; }
        public string CityName { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;
        public int TotalApplicants { get; set; }
        public int AvailableBeds { get; set; }
        public int AcceptedCount { get; set; }
        public int WaitlistCount { get; set; }
        public List<CoordinationPreviewStudentViewModel> TopStudents { get; set; } = new();
    }

    public class CoordinationPreviewStudentViewModel
    {
        public string Name { get; set; } = null!;
        public decimal Score { get; set; }
    }

    public class CoordinationResultsViewModel
    {
        public int DormitoryCityID { get; set; }
        public string CityName { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;
        public int Total { get; set; }
        public int AcceptedCount { get; set; }
        public int WaitlistCount { get; set; }
        public int RejectedCount { get; set; }
        public List<CoordinationResultRowViewModel> Results { get; set; } = new();
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    public class CoordinationResultRowViewModel
    {
        public int ID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string? Faculty { get; set; }
        public decimal? TotalScore { get; set; }
        public int? Rank { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? ProcessedAt { get; set; }
    }

    public class SpecialCasesViewModel
    {
        public int DormitoryCityID { get; set; }
        public string CityName { get; set; } = null!;
        public List<SpecialCaseRowViewModel> SpecialCases { get; set; } = new();
        public AddSpecialCaseViewModel NewSpecialCase { get; set; } = new();
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    public class SpecialCaseRowViewModel
    {
        public int ID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string CaseType { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string? ReviewNotes { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AddSpecialCaseViewModel
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "نوع الحالة مطلوب")]
        public string CaseType { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public int ApplicationID { get; set; }
    }

    public class ReviewSpecialCaseViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        public string Status { get; set; } = null!;

        public string? ReviewNotes { get; set; }
    }

    public class WaitlistViewModel
    {
        public int DormitoryCityID { get; set; }
        public string CityName { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;
        public int TotalWaitlisted { get; set; }
        public List<WaitlistRowViewModel> Waitlisted { get; set; } = new();
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    public class WaitlistRowViewModel
    {
        public int ID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public decimal? TotalScore { get; set; }
        public int? Rank { get; set; }
    }

    public class FacultyQuotasViewModel
    {
        public int DormitoryCityID { get; set; }
        public string CityName { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;
        public List<FacultyQuotaRowViewModel> Quotas { get; set; } = new();
        public AddFacultyQuotaViewModel NewQuota { get; set; } = new();
    }

    public class FacultyQuotaRowViewModel
    {
        public int ID { get; set; }
        public string Faculty { get; set; } = null!;
        public int MaxQuota { get; set; }
        public int MinQuota { get; set; }
        public int CurrentCount { get; set; }
        public bool IsFull => CurrentCount >= MaxQuota;
    }

    public class ManualOverrideViewModel
    {
        public int ID { get; set; }
        public string StudentName { get; set; } = null!;
        public string? NationalID { get; set; }
        public string? Faculty { get; set; }
        public decimal? DistanceScore { get; set; }
        public decimal? GradeScore { get; set; }
        public decimal? AgeScore { get; set; }
        public decimal? SpecialBonus { get; set; }
        public decimal? TotalScore { get; set; }
        public int? Rank { get; set; }
    }

    public class ManualOverrideSaveViewModel
    {
        [Required]
        public int ID { get; set; }

        [Range(0, 100)]
        public decimal? DistanceScore { get; set; }

        [Range(0, 100)]
        public decimal? GradeScore { get; set; }

        [Range(0, 100)]
        public decimal? AgeScore { get; set; }

        [Range(0, 100)]
        public decimal? SpecialBonus { get; set; }
    }

    public class AddFacultyQuotaViewModel
    {
        [Required(ErrorMessage = "الكلية مطلوبة")]
        public string Faculty { get; set; } = null!;

        [Required]
        [Range(1, 10000)]
        public int MaxQuota { get; set; }

        [Range(0, 10000)]
        public int MinQuota { get; set; }
    }
}
