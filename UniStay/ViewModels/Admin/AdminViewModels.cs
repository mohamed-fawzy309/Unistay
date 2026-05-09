using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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

    public class ReviewDecisionViewModel
    {
        [Required(ErrorMessage = "يرجى اختيار القرار")]
        public string Decision { get; set; } = null!;

        public string? RejectionReason { get; set; }

        public string? AdminNotes { get; set; }
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

        [Range(0, 9999)]
        public decimal? DistanceFromUniv { get; set; }

        [Range(0, 100)]
        public decimal? GradePercentage { get; set; }

        public bool? HasMedicalCondition { get; set; }

        [StringLength(500)]
        public string? MedicalDescription { get; set; }
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
}
