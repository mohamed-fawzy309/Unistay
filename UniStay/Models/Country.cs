namespace UniStay.Models;

public partial class Country
{
    public int ID { get; set; }
    public string Name { get; set; } = null!;
    public string? NameAr { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
}
