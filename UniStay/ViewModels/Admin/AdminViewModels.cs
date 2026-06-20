using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using UniStay.ViewModels.Permissions;

namespace UniStay.ViewModels.Admin
{
    // ══════════════════════════════════════════════════════════════
    // 1. Application Management
    // ══════════════════════════════════════════════════════════════

    public class ApplicationRowViewModel
    {
        public int ID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string? Faculty { get; set; }
        public string CityName { get; set; } = null!;
        public string StudentType { get; set; } = null!;
        public string HousingType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedByName { get; set; }
        public string ServerVerificationStatus { get; set; } = null!;
    }

    // ══════════════════════════════════════════════════════════════
    // 2. Student Management
    // ══════════════════════════════════════════════════════════════

    public class StudentRowViewModel
    {
        public int ID { get; set; }
        public string FullName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string? Faculty { get; set; }
        public byte? AcademicYear { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LatestApplicationStatus { get; set; }
        public string? CityName { get; set; }
        public string? BuildingName { get; set; }
        public string? RoomNumber { get; set; }
        public byte? BedNumber { get; set; }
        public string? HousingStatus { get; set; }
    }

    public class EditStudentViewModel
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(200)]
        public string FullName { get; set; } = null!;

        [StringLength(100)]
        public string? Faculty { get; set; }

        [Range(1, 7)]
        public int? AcademicYear { get; set; }

        [Required]
        [Phone]
        public string? Phone { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? Governorate { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Markaz { get; set; }

        [Range(0, 9999)]
        public decimal? DistanceFromUniv { get; set; }

        [Range(0, 100)]
        public decimal? GradePercentage { get; set; }

        public bool? HasMedicalCondition { get; set; }

        [StringLength(500)]
        public string? MedicalDescription { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 2b. Student Statement (بيان حالة)
    // ══════════════════════════════════════════════════════════════

    public class StudentStatementViewModel
    {
        public StudentBasicInfo BasicInfo { get; set; } = null!;
        public StudentHousingInfo? CurrentHousing { get; set; }
        public List<PaymentRow> Payments { get; set; } = new();
        public List<AbsenceRow> Absences { get; set; } = new();
        public List<ViolationRow> Violations { get; set; } = new();
        public List<ApplicationRow> Applications { get; set; } = new();
    }

    public class StudentBasicInfo
    {
        public int ID { get; set; }
        public string FullName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string? Faculty { get; set; }
        public byte? AcademicYear { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Governorate { get; set; }
        public string? Markaz { get; set; }
        public string? City { get; set; }
        public decimal? GradePercentage { get; set; }
        public bool IsActive { get; set; }
    }

    public class StudentHousingInfo
    {
        public string CityName { get; set; } = null!;
        public string BuildingName { get; set; } = null!;
        public string RoomNumber { get; set; } = null!;
        public byte BedNumber { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Status { get; set; } = null!;
    }

    public class PaymentRow
    {
        public int ID { get; set; }
        public string PaymentType { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? RecordedAt { get; set; }
    }

    public class AbsenceRow
    {
        public int ID { get; set; }
        public DateOnly AbsenceDate { get; set; }
        public string AbsenceType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Reason { get; set; }
    }

    public class ViolationRow
    {
        public int ID { get; set; }
        public string ViolationType { get; set; } = null!;
        public string? Description { get; set; }
        public string Severity { get; set; } = null!;
        public decimal? FineAmount { get; set; }
        public string Status { get; set; } = null!;
    }

    public class ApplicationRow
    {
        public int ID { get; set; }
        public string AcademicYear { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 2c. Social Cases ViewModels
    // ══════════════════════════════════════════════════════════════

    public class AdminSocialCaseViewModel
    {
        public List<AdminSocialCaseRow> Cases { get; set; } = new();
        public int TotalCases { get; set; }
        public int OpenCases { get; set; }
        public int ResolvedCases { get; set; }
        public int HighPriority { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public string? Search { get; set; }
        public string? CaseType { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
    }

    public class AdminSocialCaseRow
    {
        public int ID { get; set; }
        public int StudentID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string? Faculty { get; set; }
        public string CaseType { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string Priority { get; set; } = null!;
        public string? AssignedTo { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 3. Cities & Buildings
    // ══════════════════════════════════════════════════════════════

    public class CreateCityViewModel
    {
        [Required(ErrorMessage = "اسم المدينة مطلوب")]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        public int UniversityID { get; set; }

        [Required(ErrorMessage = "نوع المدينة مطلوب")]
        public string CityType { get; set; } = null!;

        [StringLength(300)]
        public string? Location { get; set; }
    }

    public class EditCityViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        public string CityType { get; set; } = null!;

        [StringLength(300)]
        public string? Location { get; set; }

        public bool IsActive { get; set; }
    }

    public class BuildingRowViewModel
    {
        public int ID { get; set; }
        public string BuildingName { get; set; } = null!;
        public string BuildingType { get; set; } = null!;
        public byte FloorCount { get; set; }
        public string CityName { get; set; } = null!;
        public int CityID { get; set; }
        public int RoomCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateBuildingViewModel
    {
        [Required(ErrorMessage = "المدينة مطلوبة")]
        public int DormitoryCityID { get; set; }

        [Required(ErrorMessage = "اسم المبنى مطلوب")]
        [StringLength(100)]
        public string BuildingName { get; set; } = null!;

        [Required(ErrorMessage = "نوع المبنى مطلوب")]
        public string BuildingType { get; set; } = null!;

        [Range(1, 20, ErrorMessage = "عدد الأدوار بين 1 و 20")]
        public int FloorCount { get; set; } = 1;
    }

    public class EditBuildingViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(100)]
        public string BuildingName { get; set; } = null!;

        [Required]
        public string BuildingType { get; set; } = null!;

        [Range(1, 20)]
        public int FloorCount { get; set; }

        public bool IsActive { get; set; }
    }

    public class RoomRowViewModel
    {
        public int ID { get; set; }
        public string RoomNumber { get; set; } = null!;
        public byte FloorNumber { get; set; }
        public byte BedsCount { get; set; }
        public byte CurrentOccupancy { get; set; }
        public string? RoomType { get; set; }
        public string BuildingName { get; set; } = null!;
        public int BuildingID { get; set; }
        public string CityName { get; set; } = null!;
        public bool HasAC { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateRoomViewModel
    {
        [Required(ErrorMessage = "المبنى مطلوب")]
        public int CityBuildingID { get; set; }

        [Required(ErrorMessage = "رقم الغرفة مطلوب")]
        [StringLength(20)]
        public string RoomNumber { get; set; } = null!;

        [Range(0, 20, ErrorMessage = "رقم الدور غير صحيح")]
        public int FloorNumber { get; set; }

        [Range(1, 8, ErrorMessage = "عدد الأسرة بين 1 و 8")]
        public int BedsCount { get; set; } = 4;

        public string? RoomType { get; set; }

        public bool HasAC { get; set; }
        public bool HasBalcony { get; set; }
        public bool HasPrivateBathroom { get; set; }
        public bool HasFridge { get; set; }
    }

    public class EditRoomViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(20)]
        public string RoomNumber { get; set; } = null!;

        [Range(0, 20)]
        public int FloorNumber { get; set; }

        [Range(1, 8)]
        public int BedsCount { get; set; }

        public string? RoomType { get; set; }

        public bool HasAC { get; set; }
        public bool HasBalcony { get; set; }
        public bool HasPrivateBathroom { get; set; }
        public bool HasFridge { get; set; }

        public bool IsActive { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 4. Schedules & Instructions
    // ══════════════════════════════════════════════════════════════

    public class CreateScheduleViewModel
    {
        [Required]
        public int DormitoryCityID { get; set; }

        [Required(ErrorMessage = "العام الدراسي مطلوب")]
        [StringLength(10)]
        public string AcademicYear { get; set; } = null!;

        public DateOnly? NewStudentsOpenDate { get; set; }
        public DateOnly? NewStudentsCloseDate { get; set; }
        public DateOnly? ReturningStudentsOpenDate { get; set; }
        public DateOnly? ReturningStudentsCloseDate { get; set; }

        public bool IsOpen { get; set; } = true;
    }

    public class EditScheduleViewModel
    {
        [Required]
        public int ID { get; set; }

        public DateOnly? NewStudentsOpenDate { get; set; }
        public DateOnly? NewStudentsCloseDate { get; set; }
        public DateOnly? ReturningStudentsOpenDate { get; set; }
        public DateOnly? ReturningStudentsCloseDate { get; set; }

        public bool IsOpen { get; set; } = true;
    }

    public class CreateInstructionViewModel
    {
        public int? DormitoryCityID { get; set; }

        [Required(ErrorMessage = "العنوان مطلوب")]
        [StringLength(300)]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "المحتوى مطلوب")]
        public string Content { get; set; } = null!;

        [Required]
        public string InstructionType { get; set; } = null!;
    }

    public class EditInstructionViewModel
    {
        [Required]
        public int ID { get; set; }

        public int? DormitoryCityID { get; set; }

        [Required]
        [StringLength(300)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        [Required]
        public string InstructionType { get; set; } = null!;

        [Range(0, 255)]
        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    public class CreateAnnouncementViewModel
    {
        [Required(ErrorMessage = "العنوان مطلوب")]
        [StringLength(300)]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "المحتوى مطلوب")]
        public string Body { get; set; } = null!;

        [Required]
        public string AnnouncementType { get; set; } = null!;

        public int? DormitoryCityID { get; set; }

        public string? TargetAudience { get; set; }

        public bool PublishNow { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public List<IFormFile>? Files { get; set; }
    }

    public class EditAnnouncementViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(300)]
        public string Title { get; set; } = null!;

        [Required]
        public string Body { get; set; } = null!;

        [Required]
        public string AnnouncementType { get; set; } = null!;

        public int? DormitoryCityID { get; set; }

        public string? TargetAudience { get; set; }

        public bool PublishNow { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 5. Building Layout
    // ══════════════════════════════════════════════════════════════

    public class BuildingLayoutViewModel
    {
        public int BuildingID { get; set; }
        public string BuildingName { get; set; } = null!;
        public string CityName { get; set; } = null!;
        public byte FloorCount { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds => TotalBeds - OccupiedBeds;
        public List<FloorLayoutViewModel> Floors { get; set; } = new();
    }

    public class FloorLayoutViewModel
    {
        public byte FloorNumber { get; set; }
        public List<RoomLayoutViewModel> Rooms { get; set; } = new();
    }

    public class RoomLayoutViewModel
    {
        public int RoomID { get; set; }
        public string RoomNumber { get; set; } = null!;
        public byte BedsCount { get; set; }
        public byte CurrentOccupancy { get; set; }
        public int AvailableBeds { get; set; }
        public bool IsFull { get; set; }
        public string RoomType { get; set; } = null!;
    }

    // ══════════════════════════════════════════════════════════════
    // 6. Villages
    // ══════════════════════════════════════════════════════════════

    public class VillageViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public int DormitoryCityID { get; set; }
        public string CityName { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CreateVillageViewModel
    {
        [Required(ErrorMessage = "المدينة مطلوبة")]
        public int DormitoryCityID { get; set; }

        [Required(ErrorMessage = "اسم القرية مطلوب")]
        [StringLength(200)]
        public string Name { get; set; } = null!;
    }

    public class EditVillageViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        public bool IsActive { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 7. Housing Types
    // ══════════════════════════════════════════════════════════════

    public class HousingTypeViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateHousingTypeViewModel
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class EditHousingTypeViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 8. Meal Types
    // ══════════════════════════════════════════════════════════════

    public class MealTypeViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateMealTypeViewModel
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class EditMealTypeViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 9. Fee Types
    // ══════════════════════════════════════════════════════════════

    public class FeeTypeViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string FeeCategory { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CreateFeeTypeViewModel
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "التصنيف مطلوب")]
        public string FeeCategory { get; set; } = null!;
    }

    public class EditFeeTypeViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string FeeCategory { get; set; } = null!;

        public bool IsActive { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 10. Fee Configurations
    // ══════════════════════════════════════════════════════════════

    public class FeeConfigurationViewModel
    {
        public int ID { get; set; }
        public int FeeTypeID { get; set; }
        public string FeeTypeName { get; set; } = null!;
        public int? DormitoryCityID { get; set; }
        public string? CityName { get; set; }
        public decimal Amount { get; set; }
        public string? AcademicYear { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateFeeConfigurationViewModel
    {
        [Required(ErrorMessage = "نوع الرسم مطلوب")]
        public int FeeTypeID { get; set; }

        public int? DormitoryCityID { get; set; }

        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(0, 999999)]
        public decimal Amount { get; set; }

        [StringLength(10)]
        public string? AcademicYear { get; set; }
    }

    public class EditFeeConfigurationViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [Range(0, 999999)]
        public decimal Amount { get; set; }

        public bool IsActive { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 11. Countries
    // ══════════════════════════════════════════════════════════════

    public class CountryViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public string? NameAr { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCountryViewModel
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(200)]
        public string? NameAr { get; set; }

        [StringLength(10)]
        public string? Code { get; set; }
    }

    public class EditCountryViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(200)]
        public string? NameAr { get; set; }

        [StringLength(10)]
        public string? Code { get; set; }

        public bool IsActive { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 12. Student Categories
    // ══════════════════════════════════════════════════════════════

    public class StudentCategoryViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateStudentCategoryViewModel
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class EditStudentCategoryViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 13. Application Configuration
    // ══════════════════════════════════════════════════════════════

    public class AppConfigViewModel
    {
        public int ID { get; set; }
        public string ConfigKey { get; set; } = null!;
        public string ConfigValue { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateAppConfigViewModel
    {
        [Required(ErrorMessage = "المفتاح مطلوب")]
        [StringLength(100)]
        public string ConfigKey { get; set; } = null!;

        [Required(ErrorMessage = "القيمة مطلوبة")]
        [StringLength(2000)]
        public string ConfigValue { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? Category { get; set; }
    }

    public class EditAppConfigViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(2000)]
        public string ConfigValue { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 14. Roles
    // ══════════════════════════════════════════════════════════════

    public class RoleViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
    }

    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "اسم الدور مطلوب")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class EditRoleViewModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 15. Dashboard
    // ══════════════════════════════════════════════════════════════

    public class DashboardViewModel
    {
        public int PendingCount { get; set; }
        public int AcceptedCount { get; set; }
        public int RejectedCount { get; set; }
        public int TotalStudents { get; set; }
        public int AllocatedCount { get; set; }
        public int CityCount { get; set; }
        public int BuildingCount { get; set; }
        public int RoomCount { get; set; }
        public int UserCount { get; set; }
        public int AdminCount { get; set; }
        public int RoleCount { get; set; }
        public int TodayApplications { get; set; }
        public int TotalApplications { get; set; }
        public List<ApplicationRowViewModel> LatestApplications { get; set; } = new();
        public List<AuditLogRowViewModel> RecentAuditLogs { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════
    // 16. Student Operations
    // ══════════════════════════════════════════════════════════════

    public class CorrectNationalIdViewModel
    {
        [Required]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "الرقم القومي الجديد مطلوب")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "الرقم القومي 14 رقم")]
        public string NewNationalID { get; set; } = null!;

        public string? Reason { get; set; }
    }

    public class ChangeStudentNumberViewModel
    {
        [Required]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "رقم الجلوس الجديد مطلوب")]
        [StringLength(50)]
        public string NewStudentCode { get; set; } = null!;

        public string? Reason { get; set; }
    }

    public class ReverseAcceptanceViewModel
    {
        [Required]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "السبب مطلوب")]
        public string Reason { get; set; } = null!;
    }

    public class TransferUniversityViewModel
    {
        [Required]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "الجامعة الجديدة مطلوبة")]
        public int NewUniversityID { get; set; }

        public string? Reason { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // 17. Role Permissions
    // ══════════════════════════════════════════════════════════════

    public class SaveRolePermissionsRequest
    {
        public int RoleID { get; set; }
        public List<UniStay.ViewModels.Permissions.PermissionSaveItem> Permissions { get; set; } = new();
    }
}
