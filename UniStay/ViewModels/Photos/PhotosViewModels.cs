using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Photos;

public class PhotoIndexViewModel
{
    public List<StudentPhotoRowViewModel> Students { get; set; } = new();
    public PhotoFilterViewModel Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int WithPhotoCount { get; set; }
    public int WithoutPhotoCount { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
}

public class PhotoFilterViewModel
{
    public string? Search { get; set; }
    public int? CityID { get; set; }
    public string? PhotoStatus { get; set; }
}

public class StudentPhotoRowViewModel
{
    public int StudentID { get; set; }
    public string FullName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? Faculty { get; set; }
    public string? CityName { get; set; }
    public string? PhotoPath { get; set; }
    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
}

public class UploadPhotoViewModel
{
    [Required(ErrorMessage = "الملف مطلوب")]
    public IFormFile PhotoFile { get; set; } = null!;
}

public class BulkImportViewModel
{
    [Required(ErrorMessage = "ملف ZIP مطلوب")]
    public IFormFile ZipFile { get; set; } = null!;

    public string? MatchBy { get; set; }
}

public class BulkImportResultViewModel
{
    public int TotalInZip { get; set; }
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public int MissingCount { get; set; }
    public int DuplicateCount { get; set; }
    public List<ImportRowResult> Details { get; set; } = new();
}

public class ImportRowResult
{
    public string FileName { get; set; } = "";
    public string? MatchedStudent { get; set; }
    public string? MatchedNationalID { get; set; }
    public string Status { get; set; } = "";
    public string? ErrorMessage { get; set; }
}

public class CardIndexViewModel
{
    public List<CardQueueRowViewModel> QueuedItems { get; set; } = new();
    public CardFilterViewModel Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int PendingCount { get; set; }
    public int PrintedCount { get; set; }
    public int FailedCount { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
}

public class CardFilterViewModel
{
    public string? Search { get; set; }
    public int? CityID { get; set; }
    public string? Status { get; set; }
    public int? BuildingID { get; set; }
    public int? RoomID { get; set; }
}

public class CardQueueRowViewModel
{
    public int QueueID { get; set; }
    public int StudentID { get; set; }
    public string StudentName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? Faculty { get; set; }
    public string? CityName { get; set; }
    public string? Status { get; set; }
    public DateTime? QueuedAt { get; set; }
    public DateTime? PrintedAt { get; set; }
    public string? PrintedByName { get; set; }
}

public class PrintSelectionViewModel
{
    public List<SelectableStudentRow> Students { get; set; } = new();
    public SelectionFilterViewModel Filter { get; set; } = new();
    public List<CityLookup> Cities { get; set; } = new();
    public List<BuildingLookup> Buildings { get; set; } = new();
    public List<RoomLookup> Rooms { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
}

public class SelectionFilterViewModel
{
    public string? Search { get; set; }
    public int? CityID { get; set; }
    public int? BuildingID { get; set; }
    public int? RoomID { get; set; }
    public bool? HasPhoto { get; set; }
}

public class SelectableStudentRow
{
    public int StudentID { get; set; }
    public bool Selected { get; set; }
    public string FullName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? Faculty { get; set; }
    public string? CityName { get; set; }
    public string? BuildingName { get; set; }
    public string? RoomNumber { get; set; }
    public bool HasPhoto { get; set; }
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
}

public class RoomLookup
{
    public int ID { get; set; }
    public string RoomNumber { get; set; } = "";
}
