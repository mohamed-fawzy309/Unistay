using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class DormitoryCity
{
    public int ID { get; set; }

    public int UniversityID { get; set; }

    public string Name { get; set; } = null!;

    public string CityType { get; set; } = null!;

    public string? Location { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public int? LastUpdatedBy { get; set; }

    public virtual ICollection<Absence> Absences { get; set; } = new List<Absence>();

    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    public virtual ICollection<ApplicationSchedule> ApplicationSchedules { get; set; } = new List<ApplicationSchedule>();

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<CardPrintQueue> CardPrintQueues { get; set; } = new List<CardPrintQueue>();

    public virtual ICollection<CityBuilding> CityBuildings { get; set; } = new List<CityBuilding>();

    public virtual ICollection<CityConfiguration> CityConfigurations { get; set; } = new List<CityConfiguration>();

    public virtual ICollection<CityStaff> CityStaffs { get; set; } = new List<CityStaff>();

    public virtual ICollection<CoordinationResult> CoordinationResults { get; set; } = new List<CoordinationResult>();

    public virtual ICollection<CoordinationRule> CoordinationRules { get; set; } = new List<CoordinationRule>();

    public virtual SystemUser? CreatedByNavigation { get; set; }

    public virtual ICollection<HousingInstruction> HousingInstructions { get; set; } = new List<HousingInstruction>();

    public virtual SystemUser? LastUpdatedByNavigation { get; set; }

    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();

    public virtual ICollection<MealBlock> MealBlocks { get; set; } = new List<MealBlock>();

    public virtual ICollection<MealCancellation> MealCancellations { get; set; } = new List<MealCancellation>();

    public virtual ICollection<MealConsumption> MealConsumptions { get; set; } = new List<MealConsumption>();

    public virtual ICollection<MealSchedule> MealSchedules { get; set; } = new List<MealSchedule>();

    public virtual ICollection<Meal> Meals { get; set; } = new List<Meal>();

    public virtual University University { get; set; } = null!;



    public virtual ICollection<Violation> Violations { get; set; } = new List<Violation>();


    public virtual ICollection<FeeConfiguration> FeeConfigurations { get; set; } = new List<FeeConfiguration>();
}
