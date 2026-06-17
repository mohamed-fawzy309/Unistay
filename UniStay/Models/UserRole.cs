namespace UniStay.Models;

public partial class UserRole
{
    public int ID { get; set; }
    public int SystemUserID { get; set; }
    public int RoleID { get; set; }
    public DateTime? AssignedAt { get; set; }
    public int? AssignedBy { get; set; }

    public virtual SystemUser SystemUser { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
    public virtual SystemUser? AssignedByNavigation { get; set; }
}
