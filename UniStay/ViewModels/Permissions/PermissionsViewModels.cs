using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Permissions
{
    // ═══════════════════════════════════════════════
    // Users
    // ═══════════════════════════════════════════════

    public class UserListViewModel
    {
        public List<UserRowViewModel> Users { get; set; } = new();
        public int TotalCount     { get; set; }
        public int ActiveCount    { get; set; }
        public int SuperAdminCount{ get; set; }
    }

    public class UserRowViewModel
    {
        public int       ID                  { get; set; }
        public string    Name                { get; set; } = "";
        public string?   Email               { get; set; }
        public string?   Phone               { get; set; }
        public string?   NationalID          { get; set; }
        public bool      IsSuperAdmin        { get; set; }
        public bool      IsActive            { get; set; }
        public bool      MustChangePassword  { get; set; }
        public DateTime? LastLoginAt         { get; set; }
        public DateTime  CreatedAt           { get; set; }
        public int       PermissionsCount    { get; set; }
        public List<string> CityRoles        { get; set; } = new();
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(200)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "بريد إلكتروني غير صحيح")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "رقم هاتف غير صحيح")]
        public string? Phone { get; set; }

        [StringLength(14, MinimumLength = 14, ErrorMessage = "الرقم القومي 14 رقم")]
        public string? NationalID { get; set; }

        public bool IsSuperAdmin { get; set; } = false;
    }

    // ═══════════════════════════════════════════════
    // Assign Permissions
    // ═══════════════════════════════════════════════

    public class AssignPermissionsViewModel
    {
        public int     UserID      { get; set; }
        public string  UserName    { get; set; } = "";
        public string? UserEmail   { get; set; }
        public bool    IsSuperAdmin{ get; set; }

        public List<PermissionGroupViewModel> Groups             { get; set; } = new();
        public List<UserPermissionDto>        CurrentPermissions { get; set; } = new();
    }

    public class PermissionGroupViewModel
    {
        public int     GroupID     { get; set; }
        public string  GroupName   { get; set; } = "";
        public string? Description { get; set; }
        public List<PermissionItemViewModel> Permissions { get; set; } = new();
    }

    public class PermissionItemViewModel
    {
        public int     PermissionID  { get; set; }
        public string  PermissionKey { get; set; } = "";
        public string  DisplayName   { get; set; } = "";
        public string? Category      { get; set; }
        public bool    CanView       { get; set; }
        public bool    CanCreate     { get; set; }
        public bool    CanEdit       { get; set; }
        public bool    CanDelete     { get; set; }
    }

    public class UserPermissionDto
    {
        public int      PermissionID  { get; set; }
        public string   PermissionKey { get; set; } = "";
        public bool     CanView       { get; set; }
        public bool     CanCreate     { get; set; }
        public bool     CanEdit       { get; set; }
        public bool     CanDelete     { get; set; }
        public DateTime GrantedAt     { get; set; }
    }

    public class SavePermissionsRequest
    {
        public int UserID { get; set; }
        public List<PermissionSaveItem> Permissions { get; set; } = new();
    }

    public class PermissionSaveItem
    {
        public int  PermissionID { get; set; }
        public bool CanView      { get; set; }
        public bool CanCreate    { get; set; }
        public bool CanEdit      { get; set; }
        public bool CanDelete    { get; set; }
    }

    // ═══════════════════════════════════════════════
    // City Roles
    // ═══════════════════════════════════════════════

    public class AssignCityRoleViewModel
    {
        public int    UserID { get; set; }

        [Required(ErrorMessage = "المدينة مطلوبة")]
        public int    DormitoryCityID { get; set; }

        [Required(ErrorMessage = "الدور مطلوب")]
        public string RoleInCity { get; set; } = "";

        public bool   IsPrimary { get; set; } = true;
    }

    public class CityRoleRowViewModel
    {
        public int      CityStaffID     { get; set; }
        public string   CityName        { get; set; } = "";
        public string   RoleInCity      { get; set; } = "";
        public string   RoleDisplayName { get; set; } = "";
        public bool     IsPrimary       { get; set; }
        public DateTime AssignedAt      { get; set; }
    }

    public record SelectItem(string Value, string Text);

    // ═══════════════════════════════════════════════
    // DataScopes
    // ═══════════════════════════════════════════════

    public class DataScopeIndexViewModel
    {
        public int    UserID   { get; set; }
        public string UserName { get; set; } = "";

        public List<UserDataScopeRowViewModel> CurrentScopes     { get; set; } = new();
        public AddDataScopeViewModel           AddScope          { get; set; } = new();
        public List<SelectItem>                AvailableCities   { get; set; } = new();
        public List<SelectItem>                AvailableBuildings{ get; set; } = new();
    }

    public class UserDataScopeRowViewModel
    {
        public int     DataScopeID      { get; set; }
        public string  ScopeType        { get; set; } = "";
        public string  ScopeTypeDisplay { get; set; } = "";
        public string? ScopeValue       { get; set; }
        public string  ScopeValueDisplay{ get; set; } = "";
    }

    public class AddDataScopeViewModel
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "نوع النطاق مطلوب")]
        public string  ScopeType  { get; set; } = "";
        public string? ScopeValue { get; set; }

        public static readonly List<SelectItem> ScopeTypes = new()
        {
            new("All",           "كل البيانات"),
            new("MaleOnly",      "ذكور فقط"),
            new("FemaleOnly",    "إناث فقط"),
            new("DormitoryCity", "مدينة جامعية محددة"),
            new("Building",      "مبنى محدد"),
            new("Faculty",       "كلية محددة"),
        };
    }

    // ═══════════════════════════════════════════════
    // Audit Log
    // ═══════════════════════════════════════════════

    public class AuditLogViewModel
    {
        public List<AuditLogRowViewModel> Logs       { get; set; } = new();
        public AuditLogFilterViewModel    Filter     { get; set; } = new();
        public int TodayCount { get; set; }
        public int WeekCount  { get; set; }
        public int TotalCount { get; set; }
    }

    public class AuditLogRowViewModel
    {
        public int      ID              { get; set; }
        public int      UserID          { get; set; }
        public string   UserType        { get; set; } = "";
        public string   UserDisplayName { get; set; } = "";
        public string   Action          { get; set; } = "";
        public string   ActionDisplay   { get; set; } = "";
        public string?  TableName       { get; set; }
        public int?     RecordID        { get; set; }
        public string?  OldValues       { get; set; }
        public string?  NewValues       { get; set; }
        public string?  IPAddress       { get; set; }
        public string?  CityName        { get; set; }
        public DateTime CreatedAt       { get; set; }

        public string ActionCategory =>
            Action.Contains("Delete") || Action.Contains("Reject") ? "danger"  :
            Action.Contains("Create") || Action.Contains("Approve")? "success" :
            Action.Contains("Edit")   || Action.Contains("Update") ? "warning" : "info";
    }

    public class AuditLogFilterViewModel
    {
        public string?   UserType  { get; set; }
        public string?   Action    { get; set; }
        public string?   TableName { get; set; }
        public int?      UserID    { get; set; }
        public DateTime? From      { get; set; }
        public DateTime? To        { get; set; }
        public int       Page      { get; set; } = 1;
        public int       PageSize  { get; set; } = 50;
    }

}
