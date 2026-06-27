using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations
{
    public class PermissionService : IPermissionService
    {
        private readonly AssuitDbContext _context;
        private readonly IAuditService _auditService;

        public PermissionService(AssuitDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public bool HasPermission(int userId, string permissionKey, string action = "CanView")
        {
            var user = _context.SystemUsers.Find(userId);
            if (user != null && user.IsSuperAdmin)
                return true;

            var userPerm = _context.UserPermissions
                .Include(p => p.Permission)
                .FirstOrDefault(p => p.SystemUserID == userId &&
                                     p.Permission!.PermissionKey == permissionKey);

            if (userPerm != null)
            {
                var overrideResult = action switch
                {
                    "CanView" => userPerm.CanView,
                    "CanCreate" => userPerm.CanCreate,
                    "CanEdit" => userPerm.CanEdit,
                    "CanDelete" => userPerm.CanDelete,
                    _ => null
                };
                if (overrideResult.HasValue)
                    return overrideResult.Value;
            }

            var rolePerms = _context.UserRoles
                .Where(ur => ur.SystemUserID == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Permission.PermissionKey == permissionKey)
                .ToList();

            foreach (var rp in rolePerms)
            {
                var result = action switch
                {
                    "CanView" => rp.CanView,
                    "CanCreate" => rp.CanCreate,
                    "CanEdit" => rp.CanEdit,
                    "CanDelete" => rp.CanDelete,
                    _ => false
                };
                if (result) return true;
            }

            return false;
        }

        public HashSet<string> GetUserPermissionKeys(int userId)
        {
            var user = _context.SystemUsers.Find(userId);
            if (user != null && user.IsSuperAdmin)
                return _context.Permissions.Select(p => p.PermissionKey).ToHashSet();

            var directKeys = _context.UserPermissions
                .Where(up => up.SystemUserID == userId && up.CanView == true)
                .Include(up => up.Permission)
                .Select(up => up.Permission!.PermissionKey);

            var roleKeys = _context.UserRoles
                .Where(ur => ur.SystemUserID == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Include(rp => rp.Permission)
                .Where(rp => rp.CanView)
                .Select(rp => rp.Permission.PermissionKey);

            return directKeys.Union(roleKeys).ToHashSet();
        }

        public int GetEffectivePermissionCount(int userId)
        {
            var user = _context.SystemUsers.Find(userId);
            if (user != null && user.IsSuperAdmin)
                return _context.Permissions.Count();

            var directKeys = _context.UserPermissions
                .Where(up => up.SystemUserID == userId && up.CanView == true)
                .Select(up => up.PermissionID);

            var roleKeys = _context.UserRoles
                .Where(ur => ur.SystemUserID == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Where(rp => rp.CanView)
                .Select(rp => rp.PermissionID);

            return directKeys.Union(roleKeys).Distinct().Count();
        }

        public bool IsInDataScope(int userId, int? cityId = null, int? buildingId = null, string? faculty = null)
        {
            var scopes = _context.DataScopes
                .Where(ds => ds.SystemUsers.Any(u => u.ID == userId))
                .ToList();

            if (scopes.Any(s => s.ScopeType == "All"))
                return true;

            if (cityId.HasValue && scopes.Any(s =>
                s.ScopeType == "DormitoryCity" &&
                s.ScopeValue == $"CityID:{cityId}"))
                return true;

            if (buildingId.HasValue && scopes.Any(s =>
                s.ScopeType == "Building" &&
                s.ScopeValue == $"BuildingID:{buildingId}"))
                return true;

            if (!string.IsNullOrEmpty(faculty) && scopes.Any(s =>
                s.ScopeType == "Faculty" &&
                s.ScopeValue == faculty))
                return true;

            return false;
        }

        public bool CanUserGrantPermission(int grantedBy, string permissionKey)
        {
            var user = _context.SystemUsers.Find(grantedBy);
            if (user != null && user.IsSuperAdmin)
                return true;

            return HasPermission(grantedBy, permissionKey, "CanEdit");
        }

        public bool CanUserManageUser(int requesterId, int targetUserId)
        {
            if (requesterId == targetUserId)
                return false;

            var targetUser = _context.SystemUsers.Find(targetUserId);
            if (targetUser == null)
                return false;

            var requester = _context.SystemUsers.Find(requesterId);
            if (requester != null && requester.IsSuperAdmin)
                return true;

            if (targetUser.IsSuperAdmin)
                return false;

            return HasPermission(requesterId, "Permissions", "CanEdit");
        }

        public bool IsPermissionInUse(int permissionId)
        {
            return _context.UserPermissions.Any(up => up.PermissionID == permissionId)
                || _context.RolePermissions.Any(rp => rp.PermissionID == permissionId);
        }

        public async Task<bool> GrantPermissionAsync(int grantedBy, int targetUserId, int permissionId, PermissionDto dto)
        {
            if (!CanUserManageUser(grantedBy, targetUserId))
                return false;

            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
                return false;

            if (!CanUserGrantPermission(grantedBy, permission.PermissionKey))
                return false;

            var existing = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.SystemUserID == targetUserId && up.PermissionID == permissionId);

            if (existing != null)
            {
                existing.CanView = dto.CanView;
                existing.CanCreate = dto.CanCreate;
                existing.CanEdit = dto.CanEdit;
                existing.CanDelete = dto.CanDelete;
            }
            else
            {
                existing = new UserPermission
                {
                    SystemUserID = targetUserId,
                    PermissionID = permissionId,
                    CanView = dto.CanView,
                    CanCreate = dto.CanCreate,
                    CanEdit = dto.CanEdit,
                    CanDelete = dto.CanDelete,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = grantedBy
                };
                _context.UserPermissions.Add(existing);
            }

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(grantedBy, "Admin", "Permission.Granted", "UserPermission", existing.ID);
            return true;
        }

        public async Task<bool> RevokePermissionAsync(int revokedBy, int targetUserId, int permissionId)
        {
            if (!CanUserManageUser(revokedBy, targetUserId))
                return false;

            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
                return false;

            if (!CanUserGrantPermission(revokedBy, permission.PermissionKey))
                return false;

            var userPerm = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.SystemUserID == targetUserId && up.PermissionID == permissionId);

            if (userPerm == null)
                return false;

            _context.UserPermissions.Remove(userPerm);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(revokedBy, "Admin", "Permission.Revoked", "UserPermission", userPerm.ID);
            return true;
        }

        public async Task<bool> RemoveRolePermissionAsync(int removedBy, int roleId, int permissionId)
        {
            var rolePerm = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleID == roleId && rp.PermissionID == permissionId);

            if (rolePerm == null)
                return false;

            _context.RolePermissions.Remove(rolePerm);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(removedBy, "Admin", "RolePermission.Removed", "RolePermission", rolePerm.ID);
            return true;
        }
    }
}
