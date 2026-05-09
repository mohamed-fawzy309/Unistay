using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Attendance
{
    public class RecordAbsenceViewModel
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "التاريخ مطلوب")]
        public DateOnly AbsenceDate { get; set; }

        public string? Reason { get; set; }
    }

    public class RequestPermissionViewModel
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        public DateOnly FromDate { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        public DateOnly ToDate { get; set; }

        [Required(ErrorMessage = "اسم ولي الأمر مطلوب")]
        [StringLength(200)]
        public string GuardianName { get; set; } = null!;

        [Required(ErrorMessage = "صلة القرابة مطلوبة")]
        [StringLength(100)]
        public string GuardianRelation { get; set; } = null!;

        [Required(ErrorMessage = "رقم هاتف ولي الأمر مطلوب")]
        [Phone]
        [StringLength(20)]
        public string GuardianPhone { get; set; } = null!;

        public string? Reason { get; set; }
    }

    public class ApprovePermissionViewModel
    {
        [Required(ErrorMessage = "يرجى تحديد القرار")]
        public string Status { get; set; } = null!;

        public string? RejectionReason { get; set; }
    }

    public class AttendanceReportViewModel
    {
        public List<AttendanceRowViewModel> Records { get; set; } = new();
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public int? DormitoryCityID { get; set; }
        public int? StudentID { get; set; }
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public List<CityLookup> Cities { get; set; } = new();
    }

    public class AttendanceRowViewModel
    {
        public int ID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public DateOnly AbsenceDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string AbsenceType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? GuardianName { get; set; }
        public string? Reason { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? ReviewedByName { get; set; }
    }

    public class CityLookup
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
    }
}
