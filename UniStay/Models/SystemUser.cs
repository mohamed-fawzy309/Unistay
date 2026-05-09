using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class SystemUser
{
    public int ID { get; set; }

    public string Name { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? NationalID { get; set; }

    public string PasswordHash { get; set; } = null!;

    public bool IsSuperAdmin { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public bool MustChangePassword { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public int? LastUpdatedBy { get; set; }

    public virtual ICollection<Absence> Absences { get; set; } = new List<Absence>();

    public virtual ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();

    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    public virtual ICollection<Application> ApplicationLastUpdatedByNavigations { get; set; } = new List<Application>();

    public virtual ICollection<Application> ApplicationReviewedByNavigations { get; set; } = new List<Application>();

    public virtual ICollection<Application> ApplicationServerVerificationByNavigations { get; set; } = new List<Application>();

    public virtual ICollection<BulkOperationLog> BulkOperationLogs { get; set; } = new List<BulkOperationLog>();

    public virtual ICollection<CardPrintQueue> CardPrintQueues { get; set; } = new List<CardPrintQueue>();

    public virtual ICollection<CityBuilding> CityBuildingCreatedByNavigations { get; set; } = new List<CityBuilding>();

    public virtual ICollection<CityBuilding> CityBuildingLastUpdatedByNavigations { get; set; } = new List<CityBuilding>();

    public virtual ICollection<CityConfiguration> CityConfigurations { get; set; } = new List<CityConfiguration>();

    public virtual ICollection<CityRoom> CityRoomCreatedByNavigations { get; set; } = new List<CityRoom>();

    public virtual ICollection<CityRoom> CityRoomLastUpdatedByNavigations { get; set; } = new List<CityRoom>();

    public virtual ICollection<CityStaff> CityStaffAssignedByNavigations { get; set; } = new List<CityStaff>();

    public virtual ICollection<CityStaff> CityStaffSystemUsers { get; set; } = new List<CityStaff>();

    public virtual ICollection<CoordinationResult> CoordinationResults { get; set; } = new List<CoordinationResult>();

    public virtual ICollection<CoordinationRule> CoordinationRules { get; set; } = new List<CoordinationRule>();

    public virtual SystemUser? CreatedByNavigation { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<DormitoryCity> DormitoryCityCreatedByNavigations { get; set; } = new List<DormitoryCity>();

    public virtual ICollection<DormitoryCity> DormitoryCityLastUpdatedByNavigations { get; set; } = new List<DormitoryCity>();

    public virtual ICollection<EvictionNotice> EvictionNotices { get; set; } = new List<EvictionNotice>();

    public virtual ICollection<HousingInstruction> HousingInstructions { get; set; } = new List<HousingInstruction>();

    public virtual ICollection<IDCard> IDCards { get; set; } = new List<IDCard>();

    public virtual ICollection<SystemUser> InverseCreatedByNavigation { get; set; } = new List<SystemUser>();

    public virtual ICollection<SystemUser> InverseLastUpdatedByNavigation { get; set; } = new List<SystemUser>();

    public virtual SystemUser? LastUpdatedByNavigation { get; set; }

    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();

    public virtual ICollection<MealBlock> MealBlocks { get; set; } = new List<MealBlock>();

    public virtual ICollection<MealCancellation> MealCancellations { get; set; } = new List<MealCancellation>();

    public virtual ICollection<MealConsumption> MealConsumptions { get; set; } = new List<MealConsumption>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<SocialCase> SocialCases { get; set; } = new List<SocialCase>();

    public virtual ICollection<SpecialCase> SpecialCases { get; set; } = new List<SpecialCase>();

    public virtual ICollection<StudentDownloadLog> StudentDownloadLogs { get; set; } = new List<StudentDownloadLog>();

    public virtual ICollection<StudentInventory> StudentInventoryAssignedByNavigations { get; set; } = new List<StudentInventory>();

    public virtual ICollection<StudentInventory> StudentInventoryReturnedByNavigations { get; set; } = new List<StudentInventory>();

    public virtual ICollection<StudentValidationLog> StudentValidationLogs { get; set; } = new List<StudentValidationLog>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<UniversityAPISync> UniversityAPISyncs { get; set; } = new List<UniversityAPISync>();

    public virtual ICollection<UserPermission> UserPermissionGrantedByNavigations { get; set; } = new List<UserPermission>();

    public virtual ICollection<UserPermission> UserPermissionSystemUsers { get; set; } = new List<UserPermission>();

    public virtual ICollection<Violation> ViolationRecordedByNavigations { get; set; } = new List<Violation>();

    public virtual ICollection<Violation> ViolationResolvedByNavigations { get; set; } = new List<Violation>();

    public virtual ICollection<DataScope> DataScopes { get; set; } = new List<DataScope>();
}
