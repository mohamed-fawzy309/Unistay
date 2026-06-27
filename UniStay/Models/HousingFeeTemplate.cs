namespace UniStay.Models;

public partial class HousingFeeTemplate
{
    public int ID { get; set; }

    public string Name { get; set; } = null!;

    public int FeeTypeID { get; set; }

    public int? DormitoryCityID { get; set; }

    public decimal Amount { get; set; }

    public string? AcademicYear { get; set; }

    public int InstallmentCount { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public int? LastUpdatedBy { get; set; }

    public virtual FeeType FeeType { get; set; } = null!;

    public virtual DormitoryCity? DormitoryCity { get; set; }

    public virtual SystemUser? CreatedByNavigation { get; set; }

    public virtual SystemUser? LastUpdatedByNavigation { get; set; }

    public virtual ICollection<StudentFeeRecord> StudentFeeRecords { get; set; } = new List<StudentFeeRecord>();
}
