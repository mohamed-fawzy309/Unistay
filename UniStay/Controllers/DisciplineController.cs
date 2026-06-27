using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Discipline;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
public class DisciplineController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly IAuditService _audit;

    public DisciplineController(AssuitDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    [HttpGet]
    public JsonResult GetViolationTypes()
    {
        var types = new List<SelectListItem>
        {
            new() { Value = "Smoking", Text = "تدخين" },
            new() { Value = "Noise", Text = "إزعاج" },
            new() { Value = "Damage", Text = "تخريب ممتلكات" },
            new() { Value = "Curfew", Text = "مخالفة حظر التجول" },
            new() { Value = "Fighting", Text = "مشاجرة" },
            new() { Value = "Alcohol", Text = "تعاطي مواد محظورة" },
            new() { Value = "UnauthorizedGuest", Text = "دخول غير مصرح به" },
            new() { Value = "Other", Text = "أخرى" }
        };
        return Json(types);
    }

    [HttpGet]
    public async Task<IActionResult> GetStudents(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return Json(new List<StudentLookupItem>());

        var students = await _db.Students
            .Where(s => s.IsDeleted != true &&
                (s.FullName.Contains(term) || s.NationalID.Contains(term)))
            .OrderBy(s => s.FullName)
            .Take(20)
            .Select(s => new StudentLookupItem
            {
                ID = s.ID,
                FullName = s.FullName,
                NationalID = s.NationalID
            })
            .ToListAsync();

        return Json(students);
    }

    [HttpGet]
    public async Task<IActionResult> BulkPermission()
    {
        ViewBag.Cities = await _db.DormitoryCities
            .Where(c => c.IsActive && !c.IsDeleted)
            .Select(c => new SelectListItem { Value = c.ID.ToString(), Text = c.Name })
            .ToListAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Attendance.Manage", "CanCreate")]
    public async Task<IActionResult> BulkPermission(BulkPermissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new SelectListItem { Value = c.ID.ToString(), Text = c.Name })
                .ToListAsync();
            return View(model);
        }

        if (model.StudentIDs == null || model.StudentIDs.Count == 0)
        {
            ModelState.AddModelError("StudentIDs", "يجب اختيار طالب واحد على الأقل");
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new SelectListItem { Value = c.ID.ToString(), Text = c.Name })
                .ToListAsync();
            return View(model);
        }

        var students = await _db.Students
            .Where(s => model.StudentIDs.Contains(s.ID) && s.IsDeleted != true)
            .ToListAsync();

        if (students.Count == 0)
        {
            ModelState.AddModelError("StudentIDs", "لا يوجد طلاب صالحين");
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new SelectListItem { Value = c.ID.ToString(), Text = c.Name })
                .ToListAsync();
            return View(model);
        }

        var now = DateTime.UtcNow;
        var absences = students.Select(s => new Absence
        {
            StudentID = s.ID,
            DormitoryCityID = model.DormitoryCityID,
            AbsenceDate = model.FromDate,
            ToDate = model.ToDate,
            AbsenceType = "Permission",
            Status = "Approved",
            RequestedBy = "Staff",
            GuardianName = model.GuardianName,
            GuardianRelation = model.GuardianRelation,
            GuardianPhone = model.GuardianPhone,
            Reason = model.Reason,
            CreatedAt = now
        }).ToList();

        _db.Absences.AddRange(absences);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Staff", "Attendance.BulkPermission",
            "Absence", null, null,
            new { model.FromDate, model.ToDate, StudentCount = absences.Count, model.DormitoryCityID });

        TempData["Success"] = $"تم إصدار {absences.Count} تصريح بنجاح";
        return RedirectToAction("BulkPermission");
    }

}
