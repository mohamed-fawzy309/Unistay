using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class MealCancellation
{
    public int ID { get; set; }

    public int? StudentID { get; set; }

    public int DormitoryCityID { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly ToDate { get; set; }

    public string CancellationType { get; set; } = null!;

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual SystemUser? CreatedByNavigation { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual Student? Student { get; set; }
}
