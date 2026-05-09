using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
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

    public async Task<IActionResult> Profile()
    {
        var user = await _db.SystemUsers.FindAsync(CurrentUserId);
        if (user == null) return RedirectToAction("Index", "Home");

        var assignedCities = await _db.CityStaffs
            .Include(cs => cs.DormitoryCity)
            .Where(cs => cs.SystemUserID == CurrentUserId)
            .ToListAsync();

        var vm = new StaffProfileViewModel
        {
            ID = user.ID,
            Name = user.Name ?? "",
            Email = user.Email ?? "",
            Phone = user.Phone ?? "",
            NationalID = user.NationalID,
            LastLoginAt = user.LastLoginAt,
            AssignedCities = assignedCities.Select(cs => new AssignedCityViewModel
            {
                CityID = cs.DormitoryCity.ID,
                CityName = cs.DormitoryCity.Name,
                CityType = cs.DormitoryCity.CityType,
                RoleInCity = cs.RoleInCity,
                IsPrimary = cs.IsPrimary
            }).ToList()
        };

        return View(vm);
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
