using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Meal;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class MealBookingController : Controller
{
    private readonly IMealBookingService _bookingService;
    private readonly IReportExportService _export;
    private readonly AssuitDbContext _db;

    public MealBookingController(IMealBookingService bookingService, IReportExportService export, AssuitDbContext db)
    {
        _bookingService = bookingService;
        _export = export;
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    [HttpGet]
    [RequirePermission("Meals.Book", "CanView")]
    public IActionResult Index()
    {
        return View(new MealBookingIndexViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Meals.Book", "CanView")]
    public async Task<IActionResult> Scan(MealBookingIndexViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.SearchTerm))
        {
            TempData["Error"] = "الرجاء إدخال بيانات الطالب";
            return RedirectToAction("Index");
        }

        var result = await _bookingService.ScanStudentAsync(model.SearchTerm);
        if (result == null)
        {
            TempData["Error"] = "الطالب غير موجود";
            return RedirectToAction("Index");
        }

        ViewBag.MealTypes = new List<string> { "Breakfast", "Lunch", "Dinner" };

        return View("ScanResult", result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Meals.Book", "CanCreate")]
    public async Task<IActionResult> Book(BookMealViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.MealTypes = new List<string> { "Breakfast", "Lunch", "Dinner" };
            TempData["Error"] = "بيانات غير صالحة";
            return RedirectToAction("Index");
        }

        var (success, message) = await _bookingService.BookMealAsync(model, CurrentUserId);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction("Index");
    }

    [HttpGet]
    [RequirePermission("Meals.Book", "CanView")]
    public async Task<IActionResult> Import()
    {
        var cities = await _db.DormitoryCities.Where(c => c.IsActive)
            .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync();
        return View(new BookingExcelImportViewModel { Cities = cities });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Meals.Book", "CanCreate")]
    public async Task<IActionResult> Import(BookingExcelImportViewModel model)
    {
        if (model.ExcelFile == null || model.ExcelFile.Length == 0)
        {
            TempData["Error"] = "الرجاء اختيار ملف";
            return RedirectToAction("Import");
        }

        if (model.ExcelFile.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" &&
            !model.ExcelFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "الرجاء رفع ملف Excel صالح (.xlsx)";
            return RedirectToAction("Import");
        }

        using var stream = model.ExcelFile.OpenReadStream();
        var result = await _bookingService.ImportFromExcelAsync(stream, model.DormitoryCityID, CurrentUserId);

        return View("ImportResult", result);
    }

    [HttpGet]
    [RequirePermission("Meals.Book", "CanView")]
    public async Task<IActionResult> GetStudentCity(int studentId)
    {
        var allocation = await _db.Allocations
            .Include(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
            .FirstOrDefaultAsync(a => a.StudentID == studentId && a.Status == "Active");

        if (allocation == null)
            return Json(new { success = false, message = "لا يوجد تخصيص نشط للطالب" });

        return Json(new { success = true, cityId = allocation.CityRoom.CityBuilding.DormitoryCityID });
    }
}
