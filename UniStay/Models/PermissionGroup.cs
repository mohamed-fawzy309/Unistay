using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class PermissionGroup
{
    public int ID { get; set; }

    public string GroupName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
