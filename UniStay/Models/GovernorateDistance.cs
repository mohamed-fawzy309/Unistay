namespace UniStay.Models;

public partial class GovernorateDistance
{
    public int ID { get; set; }

    public string GovernorateName { get; set; } = null!;

    public decimal DistanceFromUniv { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public virtual SystemUser? CreatedByNavigation { get; set; }
}
