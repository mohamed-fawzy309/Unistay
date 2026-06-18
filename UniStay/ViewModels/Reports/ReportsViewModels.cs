using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Reports;

public class StudentListsReportViewModel
{
    public List<StudentListRowViewModel> Students { get; set; } = new();
    public StudentListsFilterViewModel Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
}

public class StudentListsFilterViewModel
{
    public string? Search { get; set; }
    public int? CityID { get; set; }
    public int? BuildingID { get; set; }
    public string? Gender { get; set; }
    public string? Faculty { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
}

public class StudentListRowViewModel
{
    public int ID { get; set; }
    public string FullName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? StudentCode { get; set; }
    public string Gender { get; set; } = "";
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public decimal? GradePercentage { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? Governorate { get; set; }
    public string? Markaz { get; set; }
    public string? Status { get; set; }
    public string? AllocatedCity { get; set; }
    public string? BuildingName { get; set; }
    public string? RoomNumber { get; set; }
    public bool? HasPhoto { get; set; }
}

public class RoomOccupancyReportViewModel
{
    public List<RoomOccupancyRowViewModel> Rooms { get; set; } = new();
    public RoomOccupancyFilterViewModel Filter { get; set; } = new();
    public int TotalRooms { get; set; }
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
    public int AvailableBeds { get; set; }
    public double OccupancyRate { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
    public List<BuildingLookup> Buildings { get; set; } = new();
}

public class RoomOccupancyFilterViewModel
{
    public int? CityID { get; set; }
    public int? BuildingID { get; set; }
    public string? Floor { get; set; }
    public string? Status { get; set; }
}

public class RoomOccupancyRowViewModel
{
    public int RoomID { get; set; }
    public string CityName { get; set; } = "";
    public string BuildingName { get; set; } = "";
    public string RoomNumber { get; set; } = "";
    public int FloorNumber { get; set; }
    public int BedsCount { get; set; }
    public int CurrentOccupancy { get; set; }
    public int AvailableBeds { get; set; }
    public double OccupancyPercent { get; set; }
    public string Gender { get; set; } = "";
    public string Status { get; set; } = "";
}

public class PrintedCardsReportViewModel
{
    public List<PrintedCardRowViewModel> Cards { get; set; } = new();
    public PrintedCardsFilterViewModel Filter { get; set; } = new();
    public int TotalQueued { get; set; }
    public int TotalPrinted { get; set; }
    public int TotalPending { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
}

public class PrintedCardsFilterViewModel
{
    public int? CityID { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
}

public class PrintedCardRowViewModel
{
    public int QueueID { get; set; }
    public int StudentID { get; set; }
    public string StudentName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? CityName { get; set; }
    public string? CardNumber { get; set; }
    public string? Status { get; set; }
    public DateTime? QueuedAt { get; set; }
    public DateTime? PrintedAt { get; set; }
    public string? PrintedByName { get; set; }
}

public class StudentsWithoutPhotosViewModel
{
    public List<StudentNoPhotoRowViewModel> Students { get; set; } = new();
    public WithoutPhotosFilterViewModel Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
}

public class WithoutPhotosFilterViewModel
{
    public int? CityID { get; set; }
    public string? Gender { get; set; }
    public string? Faculty { get; set; }
    public string? Search { get; set; }
}

public class StudentNoPhotoRowViewModel
{
    public int ID { get; set; }
    public string FullName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? StudentCode { get; set; }
    public string Gender { get; set; } = "";
    public string? Faculty { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Markaz { get; set; }
}

public class MealRestrictionReportViewModel
{
    public List<MealRestrictionRowViewModel> Restrictions { get; set; } = new();
    public MealRestrictionFilterViewModel Filter { get; set; } = new();
    public int TotalBlocks { get; set; }
    public int TotalCancellations { get; set; }
    public int ActiveBlocks { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
}

public class MealRestrictionFilterViewModel
{
    public int? CityID { get; set; }
    public string? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
}

public class MealRestrictionRowViewModel
{
    public int ID { get; set; }
    public string Type { get; set; } = "";
    public string TypeDisplay { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? CityName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StudentMealHistoryViewModel
{
    public List<StudentMealRowViewModel> Meals { get; set; } = new();
    public StudentMealFilterViewModel Filter { get; set; } = new();
    public int TotalMeals { get; set; }
    public int TotalConsumed { get; set; }
    public int TotalCancelled { get; set; }
    public decimal TotalSpent { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
}

public class StudentMealFilterViewModel
{
    public int? StudentID { get; set; }
    public int? CityID { get; set; }
    public string? MealType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
}

public class StudentMealRowViewModel
{
    public int ID { get; set; }
    public string StudentName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? CityName { get; set; }
    public DateTime MealDate { get; set; }
    public string MealType { get; set; } = "";
    public decimal? Price { get; set; }
    public string Status { get; set; } = "";
    public string StatusDisplay { get; set; } = "";
    public DateTime? ScannedAt { get; set; }
}

public class SocialCaseReportViewModel
{
    public List<SocialCaseRowViewModel> Cases { get; set; } = new();
    public SocialCaseFilterViewModel Filter { get; set; } = new();
    public int TotalCases { get; set; }
    public int OpenCases { get; set; }
    public int ResolvedCases { get; set; }
    public int HighPriority { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
}

public class SocialCaseFilterViewModel
{
    public string? CaseType { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Search { get; set; }
}

public class SocialCaseRowViewModel
{
    public int ID { get; set; }
    public string StudentName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? Faculty { get; set; }
    public string CaseType { get; set; } = "";
    public string CaseTypeDisplay { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusDisplay { get; set; } = "";
    public string Priority { get; set; } = "";
    public string PriorityDisplay { get; set; } = "";
    public string? AssignedTo { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? Notes { get; set; }
}

public class CityLookup
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
}

public class BuildingLookup
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public int? CityID { get; set; }
}
