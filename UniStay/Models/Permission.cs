using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class Permission
{
    public int ID { get; set; }

    public string PermissionKey { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? Category { get; set; }

    public int? GroupID { get; set; }

    public virtual PermissionGroup? Group { get; set; }

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
