namespace UniStay.Models;

public partial class StudentPenalty
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int PenaltyTypeID { get; set; }

    public int? DormitoryCityID { get; set; }

    public decimal? FineAmount { get; set; }

    public decimal? FinePaid { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public int? RecordedBy { get; set; }

    public DateTime? RecordedAt { get; set; }

    public int? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string? ResolutionNotes { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual PenaltyType PenaltyType { get; set; } = null!;

    public virtual DormitoryCity? DormitoryCity { get; set; }

    public virtual SystemUser? RecordedByNavigation { get; set; }

    public virtual SystemUser? ResolvedByNavigation { get; set; }
}
