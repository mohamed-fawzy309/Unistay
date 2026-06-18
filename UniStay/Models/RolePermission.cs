namespace UniStay.Models;

public partial class RolePermission
{
    public int ID { get; set; }
    public int RoleID { get; set; }
    public int PermissionID { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
