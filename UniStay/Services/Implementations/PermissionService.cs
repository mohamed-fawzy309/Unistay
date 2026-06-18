using Microsoft.EntityFrameworkCore;
using System;
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

            var perm = _context.UserPermissions
                .Include(p => p.Permission)
                .FirstOrDefault(p => p.SystemUserID == userId &&
                                     p.Permission!.PermissionKey == permissionKey);

            if (perm == null) return false;

            return action switch
            {
                "CanView" => perm.CanView == true,
                "CanCreate" => perm.CanCreate == true,
                "CanEdit" => perm.CanEdit == true,
                "CanDelete" => perm.CanDelete == true,
                _ => false
            };
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
            return true; // سيتم توسيعه لاحقاً
        }
    }
}
