using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Meal;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
public class MealRestrictionsController : Controller
{
    private readonly IMealRestrictionService _restrictionService;
    private readonly IReportExportService _export;
    private readonly AssuitDbContext _db;

    public MealRestrictionsController(IMealRestrictionService restrictionService, IReportExportService export, AssuitDbContext db)
    {
        _restrictionService = restrictionService;
        _export = export;
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    [HttpGet]
    [RequirePermission("Meals.Restrict", "CanView")]
    public async Task<IActionResult> Index(string? tab, int? cityId, string? mealType, string? search, int page = 1)
    {
        var model = await _restrictionService.GetRestrictionsAsync(tab, cityId, mealType, search, page);
        return View(model);
    }

    [HttpGet]
    [RequirePermission("Meals.Restrict", "CanCreate")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Cities = await _db.DormitoryCities.Where(c => c.IsActive)
            .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync();
        ViewBag.MealTypes = await _db.Meals.Select(m => m.MealType).Distinct().ToListAsync();
        return View(new CreateRestrictionViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Meals.Restrict", "CanCreate")]
    public async Task<IActionResult> Create(CreateRestrictionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Cities = await _db.DormitoryCities.Where(c => c.IsActive)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync();
            ViewBag.MealTypes = await _db.Meals.Select(m => m.MealType).Distinct().ToListAsync();
            return View(model);
        }

        var (success, message) = await _restrictionService.CreateRestrictionAsync(model, CurrentUserId);
        if (!success)
        {
            TempData["Error"] = message;
            ViewBag.Cities = await _db.DormitoryCities.Where(c => c.IsActive)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync();
            ViewBag.MealTypes = await _db.Meals.Select(m => m.MealType).Distinct().ToListAsync();
            return View(model);
        }

        TempData["Success"] = message;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Meals.Restrict", "CanEdit")]
    public async Task<IActionResult> Remove(int id)
    {
        var (success, message) = await _restrictionService.RemoveRestrictionAsync(id, CurrentUserId);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Meals.Restrict", "CanEdit")]
    public async Task<IActionResult> RemoveExpired()
    {
        var (success, message) = await _restrictionService.RemoveExpiredRestrictionsAsync(CurrentUserId);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction("Index");
    }

    [HttpGet]
    [RequirePermission("Meals.Restrict", "CanView")]
    public async Task<IActionResult> ExportExcel(string? tab, int? cityId, string? mealType, string? search)
    {
        var model = await _restrictionService.GetRestrictionsAsync(tab, cityId, mealType, search, 1);
        var columns = new[] { "الطالب", "الرقم القومي", "المدينة", "من تاريخ", "إلى تاريخ", "نوع الوجبة", "السبب", "الحالة", "تاريخ الإنشاء" };
        var data = _export.ExportToExcel("تقرير حجب الوجبات", columns, model.Restrictions, r => new object?[] {
            r.StudentName, r.NationalID, r.CityName, r.FromDate.ToString("yyyy-MM-dd"), r.ToDate.ToString("yyyy-MM-dd"),
            r.MealType ?? "جميع الوجبات", r.Reason, r.IsActive ? "نشط" : "منتهي", r.CreatedAt?.ToString("yyyy-MM-dd HH:mm")
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MealRestrictions.xlsx");
    }

    [HttpGet]
    [RequirePermission("Meals.Restrict", "CanView")]
    public async Task<IActionResult> ExportPdf(string? tab, int? cityId, string? mealType, string? search)
    {
        var model = await _restrictionService.GetRestrictionsAsync(tab, cityId, mealType, search, 1);
        var columns = new[] { "الطالب", "الرقم القومي", "المدينة", "من تاريخ", "إلى تاريخ", "الحالة" };
        var pdfRows = model.Restrictions.Select(r => new[] {
            r.StudentName, r.NationalID, r.CityName ?? "", r.FromDate.ToString("yyyy-MM-dd"),
            r.ToDate.ToString("yyyy-MM-dd"), r.IsActive ? "نشط" : "منتهي"
        }).ToArray();
        var data = _export.ExportToPdf("تقرير حجب الوجبات", columns, pdfRows);
        return File(data, "application/pdf", "MealRestrictions.pdf");
    }

    [HttpGet]
    [RequirePermission("Meals.Restrict", "CanView")]
    public async Task<IActionResult> GetStudentInfo(string search)
    {
        var student = await _db.Students
            .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                .ThenInclude(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
            .FirstOrDefaultAsync(s => s.NationalID == search || s.StudentCode == search || s.ID.ToString() == search);

        if (student == null)
            return Json(new { success = false, message = "الطالب غير موجود" });

        var allocation = student.Allocations.FirstOrDefault();
        var cityId = allocation?.CityRoom?.CityBuilding?.DormitoryCityID ?? 0;

        return Json(new
        {
            success = true,
            studentID = student.ID,
            fullName = student.FullName,
            nationalID = student.NationalID,
            dormitoryCityID = cityId
        });
    }
}
