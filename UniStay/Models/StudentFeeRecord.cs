namespace UniStay.Models;

public partial class StudentFeeRecord
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int HousingFeeTemplateID { get; set; }

    public int? AllocationID { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public int InstallmentNumber { get; set; }

    public int TotalInstallments { get; set; }

    public DateOnly? DueDate { get; set; }

    public string Status { get; set; } = null!;

    public string? MonthYear { get; set; }

    public string? Notes { get; set; }

    public int? RecordedBy { get; set; }

    public DateTime? RecordedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual HousingFeeTemplate HousingFeeTemplate { get; set; } = null!;

    public virtual Allocation? Allocation { get; set; }

    public virtual SystemUser? RecordedByNavigation { get; set; }
}
