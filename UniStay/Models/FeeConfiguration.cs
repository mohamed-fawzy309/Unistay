namespace UniStay.Models;

public partial class FeeConfiguration
{
    public int ID { get; set; }
    public int FeeTypeID { get; set; }
    public int? DormitoryCityID { get; set; }
    public decimal Amount { get; set; }
    public string? AcademicYear { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public int? LastUpdatedBy { get; set; }

    public virtual FeeType FeeType { get; set; } = null!;
    public virtual DormitoryCity? DormitoryCity { get; set; }
    public virtual SystemUser? CreatedByNavigation { get; set; }
    public virtual SystemUser? LastUpdatedByNavigation { get; set; }
}
