using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Maintenance
{
    public class CreateMaintenanceViewModel
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "المدينة مطلوبة")]
        public int DormitoryCityID { get; set; }

        [Required(ErrorMessage = "الغرفة مطلوبة")]
        public int CityRoomID { get; set; }

        public string? Category { get; set; }

        [Required(ErrorMessage = "الوصف مطلوب")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "الأولوية مطلوبة")]
        public string Priority { get; set; } = null!;

        public List<StudentLookupItem> Students { get; set; } = new();
        public List<CityLookupItem> Cities { get; set; } = new();
        public List<RoomLookupItem> Rooms { get; set; } = new();
    }

    public class MaintenanceListViewModel
    {
        public List<MaintenanceRowViewModel> Requests { get; set; } = new();
        public string? FilterStatus { get; set; }
    }

    public class MaintenanceRowViewModel
    {
        public int ID { get; set; }
        public string StudentName { get; set; } = null!;
        public string RoomNumber { get; set; } = null!;
        public string BuildingName { get; set; } = null!;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string Priority { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public string? AssignedToName { get; set; }
    }

    public class AssignMaintenanceViewModel
    {
        [Required]
        public int RequestID { get; set; }

        [Required(ErrorMessage = "فريق الصيانة مطلوب")]
        public int StaffUserID { get; set; }
    }

    public class UpdateStatusViewModel
    {
        [Required]
        public int RequestID { get; set; }

        [Required(ErrorMessage = "الحالة الجديدة مطلوبة")]
        public string NewStatus { get; set; } = null!;
    }

    public class CompleteMaintenanceViewModel
    {
        [Required]
        public int RequestID { get; set; }

        public string? CompletionNotes { get; set; }
    }

    public class StudentLookupItem
    {
        public int ID { get; set; }
        public string FullName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
    }

    public class CityLookupItem
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
    }

    public class RoomLookupItem
    {
        public int ID { get; set; }
        public string RoomNumber { get; set; } = null!;
        public string BuildingName { get; set; } = null!;
    }
}
