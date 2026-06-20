namespace UniStay.ViewModels.Statistics;

// ─── Dashboard ───
public class StatisticsDashboardViewModel
{
    public int TotalApplicants { get; set; }
    public int AcceptedApplicants { get; set; }
    public int RejectedApplicants { get; set; }
    public int PendingApplicants { get; set; }
    public int TotalAllocated { get; set; }
    public int TotalStudents { get; set; }
    public int PrintedCards { get; set; }
    public int TodayConsumedMeals { get; set; }

    public List<ChartDataPoint> MonthlyApplications { get; set; } = new();
    public List<ChartDataPoint> ApplicationsByFaculty { get; set; } = new();
    public List<ChartDataPoint> ApplicationsByCity { get; set; } = new();
    public List<ChartDataPoint> OccupancyByCity { get; set; } = new();
    public List<ChartDataPoint> MealConsumptionByType { get; set; } = new();
    public List<ChartDataPoint> ApplicationsByStatus { get; set; } = new();
}

// ─── Chart Data ───
public class ChartDataPoint
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
    public string? Color { get; set; }
}

// ─── Module 1: Applicants Statistics ───
public class ApplicantsStatisticsViewModel
{
    public int Total { get; set; }
    public int Accepted { get; set; }
    public int Rejected { get; set; }
    public int Pending { get; set; }
    public int UnderReview { get; set; }
    public int Returned { get; set; }

    public List<ChartDataPoint> MonthlyApplications { get; set; } = new();
    public List<ChartDataPoint> ApplicationsByFaculty { get; set; } = new();
    public List<ChartDataPoint> ApplicationsByCity { get; set; } = new();
    public List<ChartDataPoint> ApplicationsByStatus { get; set; } = new();

    // Filter options
    public List<string> AcademicYears { get; set; } = new();
    public List<FilterLookup> Cities { get; set; } = new();
    public List<string> Faculties { get; set; } = new();

    // Selected filters
    public string? FilterAcademicYear { get; set; }
    public int? FilterCityId { get; set; }
    public string? FilterFaculty { get; set; }
    public DateTime? FilterFromDate { get; set; }
    public DateTime? FilterToDate { get; set; }
}

// ─── Module 2: Allocated Students Statistics ───
public class AllocatedStudentsStatisticsViewModel
{
    public int TotalAllocated { get; set; }
    public int TotalBeds { get; set; }
    public decimal OccupancyPercent { get; set; }

    public List<AllocationCityStat> ByCity { get; set; } = new();
    public List<AllocationBuildingStat> ByBuilding { get; set; } = new();
    public List<ChartDataPoint> CityDistribution { get; set; } = new();

    public List<FilterLookup> Cities { get; set; } = new();
    public int? FilterCityId { get; set; }
    public string? FilterAcademicYear { get; set; }
}

public class AllocationCityStat
{
    public string CityName { get; set; } = "";
    public int Allocated { get; set; }
    public int TotalBeds { get; set; }
    public decimal OccupancyPercent { get; set; }
}

public class AllocationBuildingStat
{
    public string BuildingName { get; set; } = "";
    public int Allocated { get; set; }
    public int Capacity { get; set; }
    public decimal OccupancyPercent { get; set; }
}

// ─── Module 3: Total Students Statistics ───
public class TotalStudentsStatisticsViewModel
{
    public int TotalStudents { get; set; }
    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
    public int ActiveAllocations { get; set; }

    public List<ChartDataPoint> ByFaculty { get; set; } = new();
    public List<ChartDataPoint> ByAcademicYear { get; set; } = new();
    public List<ChartDataPoint> ByGender { get; set; } = new();
}

// ─── Module 4: Printed Cards Statistics ───
public class PrintedCardsStatisticsViewModel
{
    public int Printed { get; set; }
    public int Pending { get; set; }
    public int Failed { get; set; }
    public int Total { get; set; }

    public List<ChartDataPoint> DailyPrinting { get; set; } = new();
    public List<ChartDataPoint> MonthlyPrinting { get; set; } = new();

    public DateTime? FilterFromDate { get; set; }
    public DateTime? FilterToDate { get; set; }
}

// ─── Module 5: Meal Consumption Statistics ───
public class MealConsumptionStatisticsViewModel
{
    public int BreakfastCount { get; set; }
    public int LunchCount { get; set; }
    public int DinnerCount { get; set; }
    public int Total { get; set; }

    public List<ChartDataPoint> DailyConsumption { get; set; } = new();
    public List<ChartDataPoint> ConsumptionByCity { get; set; } = new();
    public List<ChartDataPoint> ConsumptionByMealType { get; set; } = new();

    public List<FilterLookup> Cities { get; set; } = new();
    public int? FilterCityId { get; set; }
    public DateTime? FilterDate { get; set; }
}

// ─── Module 6: SMS Statistics (placeholder — SMS entity missing) ───

// ─── Shared ───
public class FilterLookup
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
}

// ─── Chart JSON response ───
public class ChartJsonResponse
{
    public bool Success { get; set; } = true;
    public List<ChartDataPoint> Data { get; set; } = new();
    public string? Message { get; set; }
}
