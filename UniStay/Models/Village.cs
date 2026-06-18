namespace UniStay.Models;

public partial class Village
{
    public int ID { get; set; }
    public int DormitoryCityID { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public int? LastUpdatedBy { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;
    public virtual SystemUser? CreatedByNavigation { get; set; }
    public virtual SystemUser? LastUpdatedByNavigation { get; set; }
}
