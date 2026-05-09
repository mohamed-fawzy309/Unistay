using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Meal
{
    public class MealIndexViewModel
    {
        public int DormitoryCityID { get; set; }
        public DateOnly? SelectedDate { get; set; }
        public int TotalMeals { get; set; }
        public int ConsumedCount { get; set; }
        public int CancelledCount { get; set; }
        public int BlockedCount { get; set; }
        public List<MealRowViewModel> Meals { get; set; } = new();
        public List<CityLookup> Cities { get; set; } = new();
    }

    public class MealRowViewModel
    {
        public int ID { get; set; }
        public int StudentID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string MealType { get; set; } = null!;
        public decimal Price { get; set; }
        public bool IsBooked { get; set; }
        public bool IsConsumed { get; set; }
        public bool IsActive { get; set; }
        public string? CancelReason { get; set; }
    }

    public class CityLookup
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
    }

    public class CancelIndividualViewModel
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "المدينة مطلوبة")]
        public int DormitoryCityID { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        public DateOnly FromDate { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        public DateOnly ToDate { get; set; }

        public string? Reason { get; set; }
    }

    public class CancelBulkViewModel
    {
        [Required(ErrorMessage = "المدينة مطلوبة")]
        public int DormitoryCityID { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        public DateOnly FromDate { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        public DateOnly ToDate { get; set; }

        public string? Reason { get; set; }
    }

    public class MealScheduleViewModel
    {
        public int DormitoryCityID { get; set; }
        public string MealType { get; set; } = null!;
        public List<ScheduleRowViewModel> Schedules { get; set; } = new();
        public string ViewTitle { get; set; } = null!;

        public DateOnly ScheduleDate { get; set; }
        public string? Description { get; set; }
        public decimal? SpecialPrice { get; set; }
    }

    public class ScheduleRowViewModel
    {
        public int ID { get; set; }
        public DateOnly ScheduleDate { get; set; }
        public string? Description { get; set; }
        public decimal? SpecialPrice { get; set; }
        public bool IsActive { get; set; }
    }

    public class BlockStudentViewModel
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "المدينة مطلوبة")]
        public int DormitoryCityID { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        public DateOnly FromDate { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        public DateOnly ToDate { get; set; }

        public string? Reason { get; set; }
    }

    public class ConsumeViewModel
    {
        public int? StudentID { get; set; }
        public string? SearchTerm { get; set; }
        public List<MealRowViewModel> AvailableMeals { get; set; } = new();
    }

    public class RecordConsumptionViewModel
    {
        [Required(ErrorMessage = "الوجبة مطلوبة")]
        public int MealID { get; set; }

        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "طريقة المسح مطلوبة")]
        public string ScanMethod { get; set; } = null!;
    }

    public class MealReportViewModel
    {
        public List<MealReportRowViewModel> Records { get; set; } = new();
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public int? DormitoryCityID { get; set; }
        public string? MealType { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalConsumed { get; set; }
        public int TotalCancelled { get; set; }
        public int TotalServed { get; set; }
        public List<CityLookup> Cities { get; set; } = new();
    }

    public class MealReportRowViewModel
    {
        public DateOnly Date { get; set; }
        public string MealType { get; set; } = null!;
        public int BookedCount { get; set; }
        public int ConsumedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
