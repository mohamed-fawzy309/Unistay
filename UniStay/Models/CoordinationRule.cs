using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class CoordinationRule
{
    public int ID { get; set; }

    public int DormitoryCityID { get; set; }

    public string RuleName { get; set; } = null!;

    public string RuleType { get; set; } = null!;

    public byte Priority { get; set; }

    public decimal Weight { get; set; }

    public decimal? MinGrade { get; set; }

    public decimal? MinDistance { get; set; }

    public string? StudentType { get; set; }

    public string? HousingType { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsLocked { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public virtual SystemUser? CreatedByNavigation { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;
}
