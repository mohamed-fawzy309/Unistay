namespace UniStay.Models;

public partial class EmployeeRecord
{
    public int ID { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? NationalID { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? JobTitle { get; set; }

    public string? Department { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public virtual SystemUser? CreatedByNavigation { get; set; }
}
