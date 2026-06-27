namespace UniStay.Models;

public partial class PenaltyType
{
    public int ID { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Severity { get; set; } = null!;

    public decimal? DefaultFineAmount { get; set; }

    public bool AffectsHousingEligibility { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public virtual SystemUser? CreatedByNavigation { get; set; }

    public virtual ICollection<StudentPenalty> StudentPenalties { get; set; } = new List<StudentPenalty>();
}
