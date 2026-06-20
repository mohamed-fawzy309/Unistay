using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Meal;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
public class MealPreparationController : Controller
{
    private readonly IMealPreparationService _prepService;
    private readonly IReportExportService _export;
    private readonly AssuitDbContext _db;

    public MealPreparationController(IMealPreparationService prepService, IReportExportService export, AssuitDbContext db)
    {
        _prepService = prepService;
        _export = export;
        _db = db;
    }

    [HttpGet]
    [RequirePermission("Meals.Prepare", "CanView")]
    public async Task<IActionResult> Index(DateOnly? date, int? cityId)
    {
        var model = await _prepService.GetPreparationSummaryAsync(date, cityId);
        return View(model);
    }

    [HttpGet]
    [RequirePermission("Meals.Prepare", "CanView")]
    public async Task<IActionResult> DailySheet(DateOnly? date, int? cityId)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.Today);
        var model = await _prepService.GetDailySheetAsync(d, cityId);
        return View(model);
    }

    [HttpGet]
    [RequirePermission("Meals.Prepare", "CanView")]
    public async Task<IActionResult> KitchenReport(DateOnly? date, int? cityId)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.Today);
        var model = await _prepService.GetKitchenReportAsync(d, cityId);
        return View(model);
    }

    [HttpGet]
    [RequirePermission("Meals.Prepare", "CanView")]
    public async Task<IActionResult> DistributionReport(DateOnly? date, int? cityId)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.Today);
        var model = await _prepService.GetDistributionReportAsync(d, cityId);
        return View(model);
    }

    [HttpGet]
    [RequirePermission("Meals.Prepare", "CanView")]
    public async Task<IActionResult> ExportDailySheetExcel(DateOnly? date, int? cityId)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.Today);
        var data = await _prepService.ExportDailySheetExcelAsync(d, cityId);
        var cityName = cityId.HasValue
            ? await _db.DormitoryCities.Where(c => c.ID == cityId.Value).Select(c => c.Name).FirstOrDefaultAsync() ?? ""
            : "all";
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DailySheet_{d:yyyy-MM-dd}_{cityName}.xlsx");
    }

    [HttpGet]
    [RequirePermission("Meals.Prepare", "CanView")]
    public async Task<IActionResult> ExportDailySheetPdf(DateOnly? date, int? cityId)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.Today);
        var data = await _prepService.ExportDailySheetPdfAsync(d, cityId);
        return File(data, "application/pdf", $"DailySheet_{d:yyyy-MM-dd}.pdf");
    }

    [HttpGet]
    [RequirePermission("Meals.Prepare", "CanView")]
    public async Task<IActionResult> ExportKitchenReportExcel(DateOnly? date, int? cityId)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.Today);
        var model = await _prepService.GetKitchenReportAsync(d, cityId);
        var columns = new[] { "نوع الوجبة", "عدد المعد", "عدد المستهلك", "المتبقي", "التكلفة" };
        var data = _export.ExportToExcel("تقرير المطبخ", columns, model.MealTypeSummaries, r => new object?[] {
            r.MealType, r.PreparedCount, r.ConsumedCount, r.RemainingCount, r.Cost
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"KitchenReport_{d:yyyy-MM-dd}.xlsx");
    }

    [HttpGet]
    [RequirePermission("Meals.Prepare", "CanView")]
    public async Task<IActionResult> ExportDistributionReportExcel(DateOnly? date, int? cityId)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.Today);
        var model = await _prepService.GetDistributionReportAsync(d, cityId);
        var columns = new[] { "المدينة", "عدد المعد", "عدد الموزع", "المتبقي" };
        var data = _export.ExportToExcel("تقرير التوزيع", columns, model.CitySummaries, r => new object?[] {
            r.CityName, r.PreparedCount, r.DistributedCount, r.PendingCount
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DistributionReport_{d:yyyy-MM-dd}.xlsx");
    }
}
