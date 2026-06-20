using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Meal;

public class MealReceivingIndexViewModel
{
    public string? SearchTerm { get; set; }
    public int? StudentID { get; set; }
}

public class ScanResultViewModel
{
    public int StudentID { get; set; }
    public string StudentName { get; set; } = null!;
    public string NationalID { get; set; } = null!;
    public string? Photo { get; set; }
    public string? CityName { get; set; }
    public bool IsEligible { get; set; }
    public string? EligibilityMessage { get; set; }
    public bool HasActiveRestriction { get; set; }
    public string? RestrictionReason { get; set; }
    public List<EligibleMealViewModel> AvailableMeals { get; set; } = new();
}

public class EligibleMealViewModel
{
    public int MealID { get; set; }
    public string MealType { get; set; } = null!;
    public string MealTypeDisplay { get; set; } = null!;
    public DateOnly MealDate { get; set; }
    public decimal Price { get; set; }
}

public class ConfirmReceiptViewModel
{
    [Required]
    public int MealID { get; set; }

    [Required]
    public int StudentID { get; set; }

    [Required]
    public string ScanMethod { get; set; } = null!;

    public string? RejectReason { get; set; }
}

public class ExcelImportViewModel
{
    public IFormFile? ExcelFile { get; set; }
    public int DormitoryCityID { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
}

public class ExcelImportResultViewModel
{
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int TotalRows { get; set; }
    public List<ExcelImportRowViewModel> Details { get; set; } = new();
}

public class ExcelImportRowViewModel
{
    public int RowNumber { get; set; }
    public string? StudentIDStr { get; set; }
    public string? NationalID { get; set; }
    public string? MealDate { get; set; }
    public string? MealType { get; set; }
    public string Status { get; set; } = null!;
    public string? Message { get; set; }
}
