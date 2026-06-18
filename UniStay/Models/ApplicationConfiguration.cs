namespace UniStay.Models;

public partial class ApplicationConfiguration
{
    public int ID { get; set; }
    public string ConfigKey { get; set; } = null!;
    public string ConfigValue { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public int? LastUpdatedBy { get; set; }
}
