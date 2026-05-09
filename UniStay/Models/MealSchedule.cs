using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class MealSchedule
{
    public int ID { get; set; }

    public int DormitoryCityID { get; set; }

    public DateOnly ScheduleDate { get; set; }

    public string MealType { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public decimal? SpecialPrice { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;
}
