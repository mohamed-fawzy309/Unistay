using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class UserPermission
{
    public int ID { get; set; }

    public int SystemUserID { get; set; }

    public int PermissionID { get; set; }

    public bool? CanView { get; set; }

    public bool? CanCreate { get; set; }

    public bool? CanEdit { get; set; }

    public bool? CanDelete { get; set; }

    public int? GrantedBy { get; set; }

    public DateTime? GrantedAt { get; set; }

    public virtual SystemUser? GrantedByNavigation { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual SystemUser SystemUser { get; set; } = null!;
}
