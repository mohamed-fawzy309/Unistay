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

// ─── Module 6: SMS Statistics ───
public class SmsStatisticsViewModel
{
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalFailed { get; set; }
    public int TotalPendingSms { get; set; }

    public List<ChartDataPoint> DailySmsSent { get; set; } = new();
    public List<ChartDataPoint> SmsByType { get; set; } = new();
    public List<ChartDataPoint> SmsByStatus { get; set; } = new();

    public List<SmsLogRowViewModel> RecentLogs { get; set; } = new();

    public DateTime? FilterFromDate { get; set; }
    public DateTime? FilterToDate { get; set; }
    public string? FilterStatus { get; set; }
    public string? FilterType { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalLogs { get; set; }
}

public class SmsLogRowViewModel
{
    public int ID { get; set; }
    public string RecipientName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string MessageType { get; set; } = "";
    public string MessageTypeDisplay { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusDisplay { get; set; } = "";
    public string? MessageContent { get; set; }
    public DateTime? SentAt { get; set; }
}

// ─── Shared ───
public class FilterLookup
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
}

// ─── Module 7: Custom Report Builder ───
public class CustomReportViewModel
{
    public string ReportType { get; set; } = "Students";
    public string? SelectedColumns { get; set; }
    public int? FilterCityId { get; set; }
    public string? FilterAcademicYear { get; set; }
    public string? FilterStatus { get; set; }
    public string? FilterFaculty { get; set; }
    public string? FilterGender { get; set; }
    public string? FilterGovernorate { get; set; }
    public DateTime? FilterFromDate { get; set; }
    public DateTime? FilterToDate { get; set; }
    public string? SearchTerm { get; set; }

    public List<FilterLookup> Cities { get; set; } = new();
    public List<string> AcademicYears { get; set; } = new();
    public List<string> Faculties { get; set; } = new();
    public List<string> Governorates { get; set; } = new();

    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, string>> Rows { get; set; } = new();
    public int TotalCount { get; set; }
    public bool HasRun { get; set; }
}

public static class ReportTypeInfo
{
    public static readonly Dictionary<string, ReportTypeDefinition> Types = new()
    {
        ["Students"] = new ReportTypeDefinition
        {
            DisplayName = "الطلاب",
            AvailableColumns = new Dictionary<string, string>
            {
                ["FullName"] = "الاسم", ["NationalID"] = "الرقم القومي", ["StudentCode"] = "كود الطالب",
                ["Gender"] = "النوع", ["Phone"] = "الهاتف", ["Email"] = "البريد",
                ["Faculty"] = "الكلية", ["Department"] = "القسم", ["GradeText"] = "الفرقة",
                ["GradePercentage"] = "النسبة المئوية", ["Governorate"] = "المحافظة",
                ["Markaz"] = "المركز", ["City"] = "المدينة", ["Address"] = "العنوان",
                ["DistanceFromUniv"] = "المسافة", ["IsActive"] = "نشط", ["CreatedAt"] = "تاريخ التسجيل"
            }
        },
        ["Applications"] = new ReportTypeDefinition
        {
            DisplayName = "طلبات التقديم",
            AvailableColumns = new Dictionary<string, string>
            {
                ["StudentName"] = "اسم الطالب", ["NationalID"] = "الرقم القومي",
                ["DormitoryCity"] = "المدينة", ["AcademicYear"] = "العام الدراسي",
                ["StudentType"] = "نوع الطالب", ["HousingType"] = "نوع السكن",
                ["Status"] = "الحالة", ["CoordinationScore"] = "درجة التنسيق",
                ["CreatedAt"] = "تاريخ التقديم", ["ReviewedAt"] = "تاريخ المراجعة"
            }
        },
        ["Allocations"] = new ReportTypeDefinition
        {
            DisplayName = "التسكين",
            AvailableColumns = new Dictionary<string, string>
            {
                ["StudentName"] = "اسم الطالب", ["NationalID"] = "الرقم القومي",
                ["DormitoryCity"] = "المدينة", ["Building"] = "المبنى",
                ["RoomNumber"] = "الغرفة", ["BedNumber"] = "السرير",
                ["AcademicYear"] = "العام الدراسي", ["Status"] = "الحالة",
                ["StartDate"] = "تاريخ البدء", ["EndDate"] = "تاريخ الانتهاء"
            }
        },
        ["Violations"] = new ReportTypeDefinition
        {
            DisplayName = "المخالفات",
            AvailableColumns = new Dictionary<string, string>
            {
                ["StudentName"] = "اسم الطالب", ["ViolationType"] = "نوع المخالفة",
                ["Description"] = "الوصف", ["Severity"] = "الخطورة",
                ["Status"] = "الحالة", ["FineAmount"] = "قيمة الغرامة",
                ["FinePaid"] = "المدفوع", ["RecordedAt"] = "تاريخ التسجيل"
            }
        },
        ["Penalties"] = new ReportTypeDefinition
        {
            DisplayName = "الجزاءات",
            AvailableColumns = new Dictionary<string, string>
            {
                ["StudentName"] = "اسم الطالب", ["PenaltyType"] = "نوع الجزاء",
                ["FineAmount"] = "قيمة الغرامة", ["FinePaid"] = "المدفوع",
                ["Status"] = "الحالة", ["Description"] = "الوصف",
                ["RecordedAt"] = "تاريخ التسجيل", ["ResolvedAt"] = "تاريخ الحل"
            }
        },
        ["Payments"] = new ReportTypeDefinition
        {
            DisplayName = "المدفوعات",
            AvailableColumns = new Dictionary<string, string>
            {
                ["StudentName"] = "اسم الطالب", ["PaymentType"] = "نوع الدفع",
                ["Amount"] = "المبلغ", ["PaidAmount"] = "المدفوع",
                ["Status"] = "الحالة", ["MonthYear"] = "الشهر",
                ["RecordedAt"] = "تاريخ التسجيل", ["PaidAt"] = "تاريخ الدفع"
            }
        }
    };
}

public class ReportTypeDefinition
{
    public string DisplayName { get; set; } = "";
    public Dictionary<string, string> AvailableColumns { get; set; } = new();
}

// ─── Chart JSON response ───
public class ChartJsonResponse
{
    public bool Success { get; set; } = true;
    public List<ChartDataPoint> Data { get; set; } = new();
    public string? Message { get; set; }
}
