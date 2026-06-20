using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Meal;

public class MealRestrictionIndexViewModel
{
    public string? Tab { get; set; }
    public int? CityId { get; set; }
    public string? MealType { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int ActiveCount { get; set; }
    public int ExpiredCount { get; set; }
    public int TotalCount { get; set; }
    public List<MealRestrictionRowViewModel> Restrictions { get; set; } = new();
    public List<CityLookup> Cities { get; set; } = new();
    public List<string> MealTypes { get; set; } = new();
}

public class MealRestrictionRowViewModel
{
    public int ID { get; set; }
    public int StudentID { get; set; }
    public string StudentName { get; set; } = null!;
    public string NationalID { get; set; } = null!;
    public string? CityName { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? MealType { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateRestrictionViewModel
{
    [Required(ErrorMessage = "الطالب مطلوب")]
    [Display(Name = "الطالب")]
    public int StudentID { get; set; }

    [Required(ErrorMessage = "المدينة مطلوبة")]
    [Display(Name = "المدينة")]
    public int DormitoryCityID { get; set; }

    [Required(ErrorMessage = "تاريخ البداية مطلوب")]
    [Display(Name = "تاريخ البداية")]
    public DateOnly FromDate { get; set; }

    [Display(Name = "تاريخ النهاية")]
    public DateOnly? ToDate { get; set; }

    [Display(Name = "نوع الوجبة")]
    public string? MealType { get; set; }

    [Display(Name = "سبب الحجب")]
    public string? Reason { get; set; }

    public bool IsPermanent => !ToDate.HasValue;
}

public class RemoveRestrictionViewModel
{
    [Required]
    public int ID { get; set; }
}
