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
    // Student Operations - Advanced (5 operations)
    // ══════════════════════════════════════════════════════════════

    [HttpPost("CorrectNationalId")]
    [ValidateAntiForgeryToken]
    [RequirePermission("Students.Manage", "CanEdit")]
    public async Task<IActionResult> CorrectNationalId(CorrectNationalIdViewModel model)
    {
        var student = await _db.Students.FindAsync(model.StudentID);
        if (student == null) return Json(new { success = false, message = "الطالب غير موجود" });

        var oldValue = student.NationalID;
        var exists = await _db.Students.AnyAsync(s => s.NationalID == model.NewNationalID && s.ID != model.StudentID);
        if (exists) return Json(new { success = false, message = "الرقم القومي مستخدم من قبل طالب آخر" });

        student.NationalID = model.NewNationalID;
        student.LastUpdatedAt = DateTime.UtcNow;
        student.LastUpdatedBy = CurrentUserId;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Staff", "Student.CorrectNationalID", "Student", model.StudentID,
            new { NationalID = oldValue }, new { NationalID = model.NewNationalID, Reason = model.Reason });

        return Json(new { success = true, message = "تم تصحيح الرقم القومي" });
    }

    [HttpPost("ChangeStudentNumber")]
    [ValidateAntiForgeryToken]
    [RequirePermission("Students.Manage", "CanEdit")]
    public async Task<IActionResult> ChangeStudentNumber(ChangeStudentNumberViewModel model)
    {
        var student = await _db.Students.FindAsync(model.StudentID);
        if (student == null) return Json(new { success = false, message = "الطالب غير موجود" });

        var oldValue = student.StudentCode;
        student.StudentCode = model.NewStudentCode;
        student.LastUpdatedAt = DateTime.UtcNow;
        student.LastUpdatedBy = CurrentUserId;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Staff", "Student.ChangeNumber", "Student", model.StudentID,
            new { StudentCode = oldValue }, new { StudentCode = model.NewStudentCode, Reason = model.Reason });

        return Json(new { success = true, message = "تم تغيير رقم الطالب" });
    }

    [HttpPost("ReverseAcceptance")]
    [ValidateAntiForgeryToken]
    [RequirePermission("Coordination.Manage", "CanEdit")]
    public async Task<IActionResult> ReverseAcceptance(ReverseAcceptanceViewModel model)
    {
        var app = await _db.Applications
            .FirstOrDefaultAsync(a => a.StudentID == model.StudentID && a.Status == "Accepted");
        if (app == null) return Json(new { success = false, message = "لا يوجد قبول نشط لهذا الطالب" });

        var oldStatus = app.Status;
        app.Status = "Pending";
        app.RejectionReason = model.Reason;
        app.LastUpdatedAt = DateTime.UtcNow;
        app.LastUpdatedBy = CurrentUserId;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Staff", "Application.ReverseAcceptance", "Application", app.ID,
            new { Status = oldStatus }, new { Status = "Pending", Reason = model.Reason });

        return Json(new { success = true, message = "تم إلغاء القبول" });
    }

    [HttpPost("TransferUniversity")]
    [ValidateAntiForgeryToken]
    [RequirePermission("Students.Manage", "CanEdit")]
    public async Task<IActionResult> TransferUniversity(TransferUniversityViewModel model)
    {
        var student = await _db.Students.FindAsync(model.StudentID);
        if (student == null) return Json(new { success = false, message = "الطالب غير موجود" });

        var oldUniv = student.Faculty;
        var newUniv = await _db.Universities.FindAsync(model.NewUniversityID);
        if (newUniv == null) return Json(new { success = false, message = "الجامعة غير موجودة" });

        student.Faculty = newUniv.Name;
        student.LastUpdatedAt = DateTime.UtcNow;
        student.LastUpdatedBy = CurrentUserId;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Staff", "Student.TransferUniversity", "Student", model.StudentID,
            new { Faculty = oldUniv }, new { Faculty = newUniv.Name, Reason = model.Reason });

        return Json(new { success = true, message = "تم تحويل الطالب" });
    }

    [HttpPost("ResetStudentPassword")]
    [ValidateAntiForgeryToken]
    [RequirePermission("Students.Manage", "CanEdit")]
    public async Task<IActionResult> ResetStudentPassword(int studentId)
    {
        var login = await _db.StudentLogins.FirstOrDefaultAsync(l => l.StudentID == studentId);
        if (login == null) return Json(new { success = false, message = "لا يوجد حساب للطالب" });

        login.PasswordHash = _passwordService.HashPassword(login.Username);
        login.MustChangePassword = true;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Staff", "Student.ResetPassword", "StudentLogin", studentId,
            null, new { ResetTo = "Username" });

        return Json(new { success = true, message = "تم إعادة تعيين كلمة المرور للرقم الجامعي" });
    }

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
