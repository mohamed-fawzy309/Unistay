namespace UniStay.Services.Interfaces
{
    public class PermissionDto
    {
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public interface IPermissionService
    {
        bool HasPermission(int userId, string permissionKey, string action = "CanView");
        HashSet<string> GetUserPermissionKeys(int userId);
        int GetEffectivePermissionCount(int userId);
        bool IsInDataScope(int userId, int? cityId = null, int? buildingId = null, string? faculty = null);
        Task<bool> GrantPermissionAsync(int grantedBy, int targetUserId, int permissionId, PermissionDto dto);
        Task<bool> RevokePermissionAsync(int revokedBy, int targetUserId, int permissionId);
        bool CanUserGrantPermission(int grantedBy, string permissionKey);
        bool CanUserManageUser(int requesterId, int targetUserId);
        bool IsPermissionInUse(int permissionId);
        Task<bool> RemoveRolePermissionAsync(int removedBy, int roleId, int permissionId);
    }
}
