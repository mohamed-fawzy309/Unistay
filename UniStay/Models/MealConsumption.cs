using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class MealConsumption
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int MealID { get; set; }

    public int DormitoryCityID { get; set; }

    public DateOnly MealDate { get; set; }

    public string ScanMethod { get; set; } = null!;

    public DateTime? ConsumedAt { get; set; }

    public int? RecordedBy { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual Meal Meal { get; set; } = null!;

    public virtual SystemUser? RecordedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
