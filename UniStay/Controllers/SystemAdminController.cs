using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Admin;

namespace UniStay.Controllers;

[Route("SystemAdmin")]
[AdminAuthorize]
public class SystemAdminController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly IPermissionService _perm;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;

    public SystemAdminController(
        AssuitDbContext db,
        IPermissionService perm,
        IPasswordService passwordService,
        IAuditService audit,
        IEmailService email)
    {
        _db = db;
        _perm = perm;
        _passwordService = passwordService;
        _audit = audit;
        _email = email;
    }

    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    // ══════════════════════════════════════════════════════════════
    // Application Types
    // ══════════════════════════════════════════════════════════════

    [HttpGet("ApplicationTypes")]
    [RequirePermission("AppConfig.Manage", "CanView")]
    public async Task<IActionResult> ApplicationTypes()
    {
        var types = await _db.ApplicationTypes
            .OrderBy(t => t.Name)
            .Select(t => new ApplicationTypeViewModel
            {
                ID = t.ID,
                Name = t.Name,
                Description = t.Description,
                IsActive = t.IsActive
            })
            .ToListAsync();

        return View(types);
    }

    [HttpPost("ApplicationTypes")]
    [ValidateAntiForgeryToken]
    [RequirePermission("AppConfig.Manage", "CanCreate")]
    public async Task<IActionResult> ApplicationTypes(CreateApplicationTypeViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction("ApplicationTypes");

        var type = new ApplicationType { Name = model.Name, Description = model.Description, IsActive = true };
        _db.ApplicationTypes.Add(type);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Admin", "ApplicationType.Create", "ApplicationType", type.ID,
            null, new { type.Name });

        TempData["Success"] = "تم إضافة نوع الطلب";
        return RedirectToAction("ApplicationTypes");
    }

    [HttpPost("EditApplicationType")]
    [ValidateAntiForgeryToken]
    [RequirePermission("AppConfig.Manage", "CanEdit")]
    public async Task<IActionResult> EditApplicationType(EditApplicationTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "بيانات غير صحيحة";
            return RedirectToAction("ApplicationTypes");
        }

        var type = await _db.ApplicationTypes.FindAsync(model.ID);
        if (type == null) return NotFound();

        type.Name = model.Name;
        type.Description = model.Description;
        type.IsActive = model.IsActive;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Admin", "ApplicationType.Edit", "ApplicationType", model.ID,
            null, new { model.Name, model.IsActive });

        TempData["Success"] = "تم تحديث نوع الطلب";
        return RedirectToAction("ApplicationTypes");
    }

    // ══════════════════════════════════════════════════════════════
    // Student Advanced Operations
    // ══════════════════════════════════════════════════════════════

    [HttpGet("StudentOperations")]
    [RequirePermission("Students.Manage", "CanEdit")]
    public async Task<IActionResult> StudentOperations(string? search = null)
    {
        ViewBag.Search = search;
        ViewBag.Universities = await _db.Universities.OrderBy(u => u.Name).ToListAsync();

        var vm = new StudentAdvancedOpsViewModel();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var query = _db.Students
                .Where(s => s.IsDeleted != true)
                .AsQueryable();

            if (search.All(char.IsDigit) && search.Length == 14)
                query = query.Where(s => s.NationalID.Contains(search));
            else if (search.All(char.IsDigit))
                query = query.Where(s => s.StudentCode != null && s.StudentCode.Contains(search));
            else
                query = query.Where(s => s.FullName.Contains(search));

            vm.Students = await query
                .Take(20)
                .Select(s => new StudentSearchRow
                {
                    ID = s.ID,
                    FullName = s.FullName,
                    NationalID = s.NationalID,
                    StudentCode = s.StudentCode,
                    Faculty = s.Faculty,
                    ApplicationStatus = _db.Applications
                        .Where(a => a.StudentID == s.ID)
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.Status)
                        .FirstOrDefault(),
                    HasLogin = _db.StudentLogins.Any(l => l.StudentID == s.ID)
                })
                .ToListAsync();
        }

        return View(vm);
    }

    // ══════════════════════════════════════════════════════════════
    // Students by Housing Type Statistics
    // ══════════════════════════════════════════════════════════════

    [HttpGet("StudentsByHousingType")]
    [RequirePermission("Statistics.View", "CanView")]
    public async Task<IActionResult> StudentsByHousingType()
    {
        var allocations = await _db.Allocations
            .Where(a => a.Status == "Active")
            .Include(a => a.Student)
            .ToListAsync();

        var housingGroups = allocations
            .GroupBy(a => a.Student != null ? "مسكن" : "بدون")
            .Select(g => new HousingTypeStatRow
            {
                HousingType = g.Key,
                Count = g.Count(),
                Percentage = 0
            })
            .ToList();

        var totalStudents = await _db.Students.CountAsync(s => s.IsDeleted != true && s.IsActive == true);
        var allocatedCount = allocations.Count;
        var unallocatedCount = totalStudents - allocatedCount;

        var result = new StudentsByHousingTypeViewModel
        {
            TotalStudents = totalStudents,
            Stats = new List<HousingTypeStatRow>
            {
                new() { HousingType = "طالب مسكن", Count = allocatedCount, Percentage = totalStudents > 0 ? Math.Round((decimal)allocatedCount / totalStudents * 100, 1) : 0 },
                new() { HousingType = "طالب غير مسكن", Count = unallocatedCount, Percentage = totalStudents > 0 ? Math.Round((decimal)unallocatedCount / totalStudents * 100, 1) : 0 }
            }
        };

        return View(result);
    }

    // ══════════════════════════════════════════════════════════════
    // Create Admin with Permissions
    // ══════════════════════════════════════════════════════════════

    [HttpGet("CreateAdmin")]
    [RequirePermission("SystemUsers.Manage", "CanCreate")]
    public async Task<IActionResult> CreateAdmin()
    {
        var permissionGroups = await _db.PermissionGroups
            .Include(g => g.Permissions)
            .OrderBy(g => g.GroupName)
            .ToListAsync();

        var vm = new CreateAdminViewModel
        {
            Permissions = permissionGroups
                .SelectMany(g => g.Permissions.Select(p => new PermissionCheckItem
                {
                    PermissionID = p.ID,
                    PermissionKey = p.PermissionKey,
                    DisplayName = p.DisplayName,
                    GroupName = g.GroupName
                }))
                .ToList()
        };

        return View(vm);
    }

    [HttpPost("CreateAdmin")]
    [ValidateAntiForgeryToken]
    [RequirePermission("SystemUsers.Manage", "CanCreate")]
    public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var groups = await _db.PermissionGroups
                .Include(g => g.Permissions)
                .OrderBy(g => g.GroupName)
                .ToListAsync();

            model.Permissions = groups
                .SelectMany(g => g.Permissions.Select(p => new PermissionCheckItem
                {
                    PermissionID = p.ID,
                    PermissionKey = p.PermissionKey,
                    DisplayName = p.DisplayName,
                    GroupName = g.GroupName
                }))
                .ToList();

            return View(model);
        }

        if (await _db.SystemUsers.AnyAsync(u => u.Email == model.Email && !u.IsDeleted))
        {
            ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم من قبل");
            return View(model);
        }

        var user = new SystemUser
        {
            Name = model.Name,
            Email = model.Email,
            Phone = model.Phone,
            NationalID = model.NationalID,
            PasswordHash = _passwordService.HashPassword(model.NationalID ?? model.Phone),
            IsSuperAdmin = false,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentUserId
        };

        _db.SystemUsers.Add(user);
        await _db.SaveChangesAsync();

        if (model.Permissions != null)
        {
            var selectedPerms = model.Permissions.Where(p => p.CanView || p.CanCreate || p.CanEdit || p.CanDelete).ToList();
            foreach (var perm in selectedPerms)
            {
                _db.UserPermissions.Add(new UserPermission
                {
                    SystemUserID = user.ID,
                    PermissionID = perm.PermissionID,
                    CanView = perm.CanView,
                    CanCreate = perm.CanCreate,
                    CanEdit = perm.CanEdit,
                    CanDelete = perm.CanDelete
                });
            }
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync(CurrentUserId, "Admin", "SystemUser.CreateWithPermissions", "SystemUser", user.ID,
            null, new { user.Name, user.Email, PermissionCount = model.Permissions?.Count(p => p.CanView || p.CanCreate || p.CanEdit || p.CanDelete) ?? 0 });

        TempData["Success"] = $"تم إنشاء المستخدم {user.Name} بنجاح مع الصلاحيات المحددة";
        return RedirectToAction("Index", "Admin");
    }
}
