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
    public int DormitoryCityID { get; set; }
    public bool IsEligible { get; set; }
    public string? EligibilityMessage { get; set; }
    public string? RestrictionReason { get; set; }
}

public class CalendarDayViewModel
{
    public DateOnly Date { get; set; }
    public int DayNumber { get; set; }
    public bool IsBooked { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsPast { get; set; }
    public bool IsCurrentMonth { get; set; }
}

public class BookMealViewModel
{
    [Required(ErrorMessage = "الطالب مطلوب")]
    public int StudentID { get; set; }

    [Required(ErrorMessage = "المدينة مطلوبة")]
    public int DormitoryCityID { get; set; }

    [Required(ErrorMessage = "تاريخ الوجبة مطلوب")]
    [Display(Name = "تاريخ الوجبة")]
    public DateOnly MealDate { get; set; }

    public string? ScanMethod { get; set; }
}

public class BookDatesViewModel
{
    [Required]
    public int StudentID { get; set; }

    [Required]
    public int DormitoryCityID { get; set; }

    public List<DateOnly> SelectedDates { get; set; } = new();
    public string? ScanMethod { get; set; }
}


