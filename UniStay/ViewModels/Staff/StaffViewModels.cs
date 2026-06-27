namespace UniStay.ViewModels.Staff;

public class StaffDashboardViewModel
{
    public int AssignedCitiesCount { get; set; }
    public int TotalStudents { get; set; }
    public int PendingMaintenanceRequests { get; set; }
    public int TodayAbsences { get; set; }
    public List<AssignedCityViewModel> AssignedCities { get; set; } = new();
    public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
}

public class AssignedCityViewModel
{
    public int CityID { get; set; }
    public string CityName { get; set; } = null!;
    public string CityType { get; set; } = null!;
    public string RoleInCity { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public int BuildingsCount { get; set; }
    public int TotalRooms { get; set; }
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
}

public class RecentActivityViewModel
{
    public string Action { get; set; } = null!;
    public string? TableName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StaffProfileViewModel
{
    public int ID { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string NationalID { get; set; } = null!;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<AssignedCityViewModel> AssignedCities { get; set; } = new();
    public List<StaffPermissionViewModel> Permissions { get; set; } = new();
    public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
    public bool CanEdit { get; set; }
}

public class StaffPermissionViewModel
{
    public string PermissionKey { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Category { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class UpdateStaffProfileViewModel
{
    public int ID { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string NationalID { get; set; } = null!;
}
