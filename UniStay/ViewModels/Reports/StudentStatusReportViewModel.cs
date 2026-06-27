namespace UniStay.ViewModels.Reports;

public class StudentStatusReportViewModel
{
    public int StudentID { get; set; }
    public string FullName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? StudentCode { get; set; }
    public string Gender { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public string? GradeText { get; set; }
    public decimal? GradePercentage { get; set; }
    public byte? AcademicYear { get; set; }
    public string? Governorate { get; set; }
    public string? Address { get; set; }
    public string? Religion { get; set; }
    public string? Nationality { get; set; }

    public string? DormitoryCityName { get; set; }
    public string? BuildingName { get; set; }
    public string? RoomNumber { get; set; }
    public byte? BedNumber { get; set; }
    public string? AllocationStatus { get; set; }
    public DateOnly? AllocationStartDate { get; set; }
    public DateOnly? AllocationEndDate { get; set; }

    public decimal TotalFees { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingAmount { get; set; }

    public int ViolationsCount { get; set; }
    public decimal ViolationsFineTotal { get; set; }
    public decimal ViolationsPaidTotal { get; set; }
    public int PenaltiesCount { get; set; }
    public decimal PenaltiesFineTotal { get; set; }
    public decimal PenaltiesPaidTotal { get; set; }

    public bool IsPrintable { get; set; } = true;
}
