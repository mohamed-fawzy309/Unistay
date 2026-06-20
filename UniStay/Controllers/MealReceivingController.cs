using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Meal;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
public class MealReceivingController : Controller
{
    private readonly IMealReceivingService _receivingService;
    private readonly IReportExportService _export;
    private readonly AssuitDbContext _db;

    public MealReceivingController(IMealReceivingService receivingService, IReportExportService export, AssuitDbContext db)
    {
        _receivingService = receivingService;
        _export = export;
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    [HttpGet]
    [RequirePermission("Meals.Receive", "CanView")]
    public IActionResult Index()
    {
        return View(new MealReceivingIndexViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Meals.Receive", "CanView")]
    public async Task<IActionResult> Scan(MealReceivingIndexViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.SearchTerm))
        {
            TempData["Error"] = "الرجاء إدخال بيانات الطالب";
            return RedirectToAction("Index");
        }

        var result = await _receivingService.ScanStudentAsync(model.SearchTerm);
        if (result == null)
        {
            TempData["Error"] = "الطالب غير موجود";
            return RedirectToAction("Index");
        }

        return View("ScanResult", result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Meals.Receive", "CanCreate")]
    public async Task<IActionResult> Confirm(ConfirmReceiptViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "بيانات غير صالحة";
            return RedirectToAction("Index");
        }

        var (success, message) = await _receivingService.ConfirmReceiptAsync(model, CurrentUserId);
        TempData[success ? "Success" : "Error"] = message;

        if (success)
            return RedirectToAction("Index");

        return RedirectToAction("Scan", new MealReceivingIndexViewModel { SearchTerm = model.StudentID.ToString() });
    }

    [HttpGet]
    [RequirePermission("Meals.Receive", "CanView")]
    public async Task<IActionResult> Import()
    {
        var cities = await _db.DormitoryCities.Where(c => c.IsActive)
            .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync();
        return View(new ExcelImportViewModel { Cities = cities });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Meals.Receive", "CanCreate")]
    public async Task<IActionResult> Import(ExcelImportViewModel model)
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
        var result = await _receivingService.ImportFromExcelAsync(stream, model.DormitoryCityID, CurrentUserId);

        return View("ImportResult", result);
    }

    [HttpGet]
    [RequirePermission("Meals.Receive", "CanView")]
    public async Task<IActionResult> ImportResultExportExcel(int cityId, DateOnly? importDate)
    {
        var date = importDate ?? DateOnly.FromDateTime(DateTime.Today);
        var consumptions = await _db.MealConsumptions
            .Include(mc => mc.Student)
            .Where(mc => mc.MealDate == date && mc.ScanMethod == "Excel" &&
                (cityId == 0 || mc.DormitoryCityID == cityId))
            .Select(mc => new
            {
                mc.Student.FullName,
                mc.Student.NationalID,
                mc.MealDate,
                mc.Meal.MealType,
                mc.ConsumedAt
            }).ToListAsync();

        var columns = new[] { "الطالب", "الرقم القومي", "التاريخ", "نوع الوجبة", "وقت الاستلام" };
        var data = _export.ExportToExcel("سجل استلام الوجبات", columns, consumptions, r => new object?[] {
            r.FullName, r.NationalID, r.MealDate.ToString("yyyy-MM-dd"), r.MealType, r.ConsumedAt?.ToString("yyyy-MM-dd HH:mm")
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MealReceiving.xlsx");
    }
}
