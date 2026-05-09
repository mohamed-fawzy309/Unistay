using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class CityStaff
{
    public int ID { get; set; }

    public int SystemUserID { get; set; }

    public int DormitoryCityID { get; set; }

    public string RoleInCity { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public DateTime? AssignedAt { get; set; }

    public int? AssignedBy { get; set; }

    public virtual SystemUser? AssignedByNavigation { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual SystemUser SystemUser { get; set; } = null!;
}
