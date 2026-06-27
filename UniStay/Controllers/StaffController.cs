using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Staff;

namespace UniStay.Controllers;

    [Authorize(AuthenticationSchemes = "StaffCookie")]
    public class StaffController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;

        public StaffController(AssuitDbContext db, IAuditService audit, IEmailService email)
        {
            _db = db;
            _audit = audit;
            _email = email;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        [RequirePermission("Dashboard.View", "CanView")]
        public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;

        var assignedCities = await _db.CityStaffs
            .Include(cs => cs.DormitoryCity)
                .ThenInclude(c => c.CityBuildings)
                    .ThenInclude(b => b.CityRooms)
            .Where(cs => cs.SystemUserID == userId)
            .ToListAsync();

        var cityIds = assignedCities.Select(cs => cs.DormitoryCityID).ToList();

        var roomIds = await _db.CityRooms
            .Where(r => r.CityBuilding != null && cityIds.Contains(r.CityBuilding.DormitoryCityID))
            .Select(r => r.ID)
            .ToListAsync();

        var vm = new StaffDashboardViewModel
        {
            AssignedCitiesCount = assignedCities.Count,
            TotalStudents = await _db.Allocations
                .Where(a => a.Status == "Active" && roomIds.Contains(a.CityRoomID))
                .Select(a => a.StudentID).Distinct().CountAsync(),
            PendingMaintenanceRequests = await _db.MaintenanceRequests.CountAsync(m => m.Status == "Pending" && cityIds.Contains(m.DormitoryCityID)),
            TodayAbsences = await _db.Absences.CountAsync(a => a.AbsenceDate == DateOnly.FromDateTime(DateTime.UtcNow) && cityIds.Contains(a.DormitoryCityID)),
            AssignedCities = assignedCities.Select(cs => new AssignedCityViewModel
            {
                CityID = cs.DormitoryCity.ID,
                CityName = cs.DormitoryCity.Name,
                CityType = cs.DormitoryCity.CityType,
                RoleInCity = cs.RoleInCity,
                IsPrimary = cs.IsPrimary,
                BuildingsCount = cs.DormitoryCity.CityBuildings?.Count ?? 0,
                TotalRooms = cs.DormitoryCity.CityBuildings?.Sum(b => b.CityRooms?.Count ?? 0) ?? 0,
                TotalBeds = cs.DormitoryCity.CityBuildings?.Sum(b => b.CityRooms?.Sum(r => r.BedsCount) ?? 0) ?? 0,
                OccupiedBeds = cs.DormitoryCity.CityBuildings?.Sum(b => b.CityRooms?.Sum(r => r.CurrentOccupancy) ?? 0) ?? 0
            }).ToList(),
            RecentActivities = await _db.AuditLogs
                .Where(a => a.UserID == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new RecentActivityViewModel
                {
                    Action = a.Action,
                    TableName = a.TableName,
                    CreatedAt = a.CreatedAt
                }).ToListAsync()
        };

        return View(vm);
    }

    public async Task<IActionResult> Profile(int? id)
    {
        int targetUserId = id ?? CurrentUserId;
        var canEdit = id == null || id == CurrentUserId || User.IsInRole("SuperAdmin");

        var user = await _db.SystemUsers.FindAsync(targetUserId);
        if (user == null) return RedirectToAction("Index", "Home");

        var assignedCities = await _db.CityStaffs
            .Include(cs => cs.DormitoryCity)
            .Where(cs => cs.SystemUserID == targetUserId)
            .ToListAsync();

        var permissions = await _db.UserPermissions
            .Include(up => up.Permission)
            .Where(up => up.SystemUserID == targetUserId)
            .Select(up => new StaffPermissionViewModel
            {
                PermissionKey = up.Permission.PermissionKey,
                DisplayName = up.Permission.DisplayName,
                Category = up.Permission.Category,
                CanView = up.CanView ?? false,
                CanCreate = up.CanCreate ?? false,
                CanEdit = up.CanEdit ?? false,
                CanDelete = up.CanDelete ?? false
            })
            .ToListAsync();

        var recentActivities = await _db.AuditLogs
            .Where(a => a.UserID == targetUserId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new RecentActivityViewModel
            {
                Action = a.Action,
                TableName = a.TableName,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var vm = new StaffProfileViewModel
        {
            ID = user.ID,
            Name = user.Name ?? "",
            Email = user.Email ?? "",
            Phone = user.Phone ?? "",
            NationalID = user.NationalID ?? "",
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            AssignedCities = assignedCities.Select(cs => new AssignedCityViewModel
            {
                CityID = cs.DormitoryCity.ID,
                CityName = cs.DormitoryCity.Name,
                CityType = cs.DormitoryCity.CityType,
                RoleInCity = cs.RoleInCity,
                IsPrimary = cs.IsPrimary
            }).ToList(),
            Permissions = permissions,
            RecentActivities = recentActivities,
            CanEdit = canEdit
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UpdateStaffProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "الرجاء تصحيح الأخطاء";
            return RedirectToAction("Profile", new { id = model.ID });
        }

        var user = await _db.SystemUsers.FindAsync(model.ID);
        if (user == null) return NotFound();

        bool isOwnProfile = model.ID == CurrentUserId;
        if (!isOwnProfile && !User.IsInRole("SuperAdmin"))
        {
            return Forbid();
        }

        user.Name = model.Name;
        user.Email = model.Email;
        user.Phone = model.Phone;
        user.NationalID = model.NationalID;
        user.LastUpdatedAt = DateTime.UtcNow;
        user.LastUpdatedBy = CurrentUserId;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Staff", "Staff.ProfileUpdated", "SystemUser", user.ID);

        TempData["Success"] = "تم تحديث الملف الشخصي بنجاح";
        return RedirectToAction("Profile", new { id = model.ID });
    }

    public async Task<IActionResult> MyCities()
    {
        var assignedCities = await _db.CityStaffs
            .Include(cs => cs.DormitoryCity)
                .ThenInclude(c => c.CityBuildings)
                    .ThenInclude(b => b.CityRooms)
            .Where(cs => cs.SystemUserID == CurrentUserId)
            .ToListAsync();

        var cities = assignedCities.Select(cs => new AssignedCityViewModel
        {
            CityID = cs.DormitoryCity.ID,
            CityName = cs.DormitoryCity.Name,
            CityType = cs.DormitoryCity.CityType,
            RoleInCity = cs.RoleInCity,
            IsPrimary = cs.IsPrimary,
            BuildingsCount = cs.DormitoryCity.CityBuildings?.Count ?? 0,
            TotalRooms = cs.DormitoryCity.CityBuildings?.Sum(b => b.CityRooms?.Count ?? 0) ?? 0,
            TotalBeds = cs.DormitoryCity.CityBuildings?.Sum(b => b.CityRooms?.Sum(r => r.BedsCount) ?? 0) ?? 0,
            OccupiedBeds = cs.DormitoryCity.CityBuildings?.Sum(b => b.CityRooms?.Sum(r => r.CurrentOccupancy) ?? 0) ?? 0
        }).ToList();

        return View(cities);
    }
}
