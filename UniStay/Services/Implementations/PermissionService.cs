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

        public async Task<bool> GrantPermissionAsync(int grantedBy, int targetUserId, int permissionId, object dto)
        {
            await _auditService.LogAsync(grantedBy, "Staff", "Permission.Granted", "UserPermission");
            return true;
        }
    }
}
