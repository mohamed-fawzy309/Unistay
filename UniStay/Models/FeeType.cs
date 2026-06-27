namespace UniStay.Models;

public partial class FeeType
{
    public int ID { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string FeeCategory { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }

    public virtual ICollection<FeeConfiguration> FeeConfigurations { get; set; } = new List<FeeConfiguration>();

    public virtual ICollection<HousingFeeTemplate> HousingFeeTemplates { get; set; } = new List<HousingFeeTemplate>();
}
