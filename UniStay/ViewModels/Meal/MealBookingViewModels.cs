using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Meal;

public class MealBookingIndexViewModel
{
    public string? SearchTerm { get; set; }
    public int? StudentID { get; set; }
}

public class ScanBookingResultViewModel
{
    public int StudentID { get; set; }
    public string StudentName { get; set; } = null!;
    public string NationalID { get; set; } = null!;
    public string? CityName { get; set; }
    public bool IsEligible { get; set; }
    public string? EligibilityMessage { get; set; }
    public string? RestrictionReason { get; set; }
}

public class BookMealViewModel
{
    [Required(ErrorMessage = "الطالب مطلوب")]
    public int StudentID { get; set; }

    [Required(ErrorMessage = "المدينة مطلوبة")]
    public int DormitoryCityID { get; set; }

    [Required(ErrorMessage = "نوع الوجبة مطلوب")]
    [Display(Name = "نوع الوجبة")]
    public string MealType { get; set; } = null!;

    [Required(ErrorMessage = "تاريخ الوجبة مطلوب")]
    [Display(Name = "تاريخ الوجبة")]
    public DateOnly MealDate { get; set; }

    public string? ScanMethod { get; set; }
}

public class BookingExcelImportViewModel
{
    public IFormFile? ExcelFile { get; set; }
    public int DormitoryCityID { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
}

public class BookingExcelImportResultViewModel
{
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int TotalRows { get; set; }
    public List<BookingExcelImportRowViewModel> Details { get; set; } = new();
}

public class BookingExcelImportRowViewModel
{
    public int RowNumber { get; set; }
    public string? StudentIDStr { get; set; }
    public string? NationalID { get; set; }
    public string? MealDate { get; set; }
    public string? MealType { get; set; }
    public string Status { get; set; } = null!;
    public string? Message { get; set; }
}
