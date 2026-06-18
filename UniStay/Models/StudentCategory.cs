namespace UniStay.Models;

public partial class StudentCategory
{
    public int ID { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
