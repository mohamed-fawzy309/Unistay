using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class Meal
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int DormitoryCityID { get; set; }

    public DateOnly MealDate { get; set; }

    public string MealType { get; set; } = null!;

    public bool? IsBooked { get; set; }

    public bool? IsConsumed { get; set; }

    public decimal Price { get; set; }

    public bool? IsActive { get; set; }

    public string? CancelReason { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual ICollection<MealConsumption> MealConsumptions { get; set; } = new List<MealConsumption>();

    public virtual Student Student { get; set; } = null!;
}
