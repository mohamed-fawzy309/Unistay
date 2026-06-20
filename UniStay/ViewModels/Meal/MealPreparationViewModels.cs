namespace UniStay.ViewModels.Meal;

public class MealPreparationIndexViewModel
{
    public DateOnly? SelectedDate { get; set; }
    public int? CityId { get; set; }
    public int BreakfastCount { get; set; }
    public int LunchCount { get; set; }
    public int DinnerCount { get; set; }
    public int TotalCount { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
    public List<CityBreakdownViewModel> CityBreakdowns { get; set; } = new();
}

public class CityBreakdownViewModel
{
    public int CityId { get; set; }
    public string CityName { get; set; } = null!;
    public int BreakfastCount { get; set; }
    public int LunchCount { get; set; }
    public int DinnerCount { get; set; }
    public int TotalCount { get; set; }
    public List<BuildingBreakdownViewModel> Buildings { get; set; } = new();
}

public class BuildingBreakdownViewModel
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;
    public int BreakfastCount { get; set; }
    public int LunchCount { get; set; }
    public int DinnerCount { get; set; }
    public int TotalCount { get; set; }
    public List<RoomBreakdownViewModel> Rooms { get; set; } = new();
}

public class RoomBreakdownViewModel
{
    public string RoomNumber { get; set; } = null!;
    public int BreakfastCount { get; set; }
    public int LunchCount { get; set; }
    public int DinnerCount { get; set; }
    public int TotalCount { get; set; }
}

public class DailyPreparationSheetViewModel
{
    public DateOnly PrepDate { get; set; }
    public string? CityName { get; set; }
    public int BreakfastCount { get; set; }
    public int LunchCount { get; set; }
    public int DinnerCount { get; set; }
    public int TotalCount { get; set; }
    public List<CityBreakdownViewModel> CityBreakdowns { get; set; } = new();
}

public class KitchenReportViewModel
{
    public DateOnly ReportDate { get; set; }
    public int TotalMealsPrepared { get; set; }
    public int TotalConsumed { get; set; }
    public int TotalRemaining { get; set; }
    public decimal TotalCost { get; set; }
    public List<KitchenMealTypeSummaryViewModel> MealTypeSummaries { get; set; } = new();
}

public class KitchenMealTypeSummaryViewModel
{
    public string MealType { get; set; } = null!;
    public int PreparedCount { get; set; }
    public int ConsumedCount { get; set; }
    public int RemainingCount { get; set; }
    public decimal Cost { get; set; }
}

public class DistributionReportViewModel
{
    public DateOnly ReportDate { get; set; }
    public int TotalPrepared { get; set; }
    public int TotalDistributed { get; set; }
    public int TotalPending { get; set; }
    public List<DistributionCitySummaryViewModel> CitySummaries { get; set; } = new();
}

public class DistributionCitySummaryViewModel
{
    public string CityName { get; set; } = null!;
    public int PreparedCount { get; set; }
    public int DistributedCount { get; set; }
    public int PendingCount { get; set; }
    public int BuildingCount { get; set; }
}
