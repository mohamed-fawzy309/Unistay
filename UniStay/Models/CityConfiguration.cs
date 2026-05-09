using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class CityConfiguration
{
    public int ID { get; set; }

    public int DormitoryCityID { get; set; }

    public decimal? StandardFee { get; set; }

    public decimal? PremiumFee { get; set; }

    public decimal? VIPFee { get; set; }

    public decimal? ForeignStudentFee { get; set; }

    public decimal? SecurityDeposit { get; set; }

    public decimal? MealFee { get; set; }

    public decimal? RamadanMealFee { get; set; }

    public decimal? ChristianMealFee { get; set; }

    public DateOnly? NewStudentsOpenDate { get; set; }

    public DateOnly? NewStudentsCloseDate { get; set; }

    public DateOnly? ReturningStudentsOpenDate { get; set; }

    public DateOnly? ReturningStudentsCloseDate { get; set; }

    public decimal? MinDistanceKm { get; set; }

    public decimal? MinGradePercentage { get; set; }

    public byte? MaxAge { get; set; }

    public bool? AutoCoordinationEnabled { get; set; }

    public string? ExcludedFaculties { get; set; }

    public string? AllowedFacultiesOnly { get; set; }

    public byte? MaxBedsPerRoom { get; set; }

    public bool? AllowStudentBedSelection { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public int? LastUpdatedBy { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual SystemUser? LastUpdatedByNavigation { get; set; }
}
