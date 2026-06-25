using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Meal;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
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

        return RedirectToAction("Calendar", new { studentId = result.StudentID });
    }

    [HttpGet]
    [RequirePermission("Meals.Book", "CanView")]
    public async Task<IActionResult> Calendar(int studentId)
    {
        var student = await _db.Students
            .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                .ThenInclude(a => a.CityRoom).ThenInclude(r => r.CityBuilding).ThenInclude(b => b.DormitoryCity)
            .FirstOrDefaultAsync(s => s.ID == studentId);

        if (student == null)
        {
            TempData["Error"] = "الطالب غير موجود";
            return RedirectToAction("Index");
        }

        var allocation = student.Allocations.FirstOrDefault();
        var cityName = allocation?.CityRoom?.CityBuilding?.DormitoryCity?.Name ?? "";
        var cityId = allocation?.CityRoom?.CityBuilding?.DormitoryCityID ?? 0;

        var today = DateOnly.FromDateTime(DateTime.Today);

        var bookedDates = await _bookingService.GetBookedDatesAsync(studentId);

        var blockedRanges = await _db.MealBlocks
            .Where(b => b.StudentID == studentId && b.IsActive == true)
            .ToListAsync();

        var now = DateTime.Today;
        var firstOfMonth = new DateTime(now.Year, now.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

        var calendarDays = new List<CalendarDayViewModel>();

        // Add leading days from previous month
        var dayOfWeek = (int)firstOfMonth.DayOfWeek;
        for (int i = dayOfWeek - 1; i >= 0; i--)
        {
            var dt = firstOfMonth.AddDays(-i - 1);
            calendarDays.Add(new CalendarDayViewModel
            {
                Date = DateOnly.FromDateTime(dt),
                DayNumber = dt.Day,
                IsCurrentMonth = false,
                IsPast = true,
                IsBooked = true,
                IsBlocked = true
            });
        }

        for (var dt = firstOfMonth; dt <= lastOfMonth; dt = dt.AddDays(1))
        {
            var date = DateOnly.FromDateTime(dt);
            var isPast = dt < DateTime.Today;
            var isBooked = bookedDates.Contains(date);
            var isBlocked = blockedRanges.Any(b => date >= b.FromDate && date <= b.ToDate);

            calendarDays.Add(new CalendarDayViewModel
            {
                Date = date,
                DayNumber = dt.Day,
                IsCurrentMonth = true,
                IsPast = isPast,
                IsBooked = isBooked,
                IsBlocked = isBlocked
            });
        }

        // Fill remaining days
        var remainingDays = 7 - (calendarDays.Count % 7);
        if (remainingDays < 7)
        {
            for (int i = 1; i <= remainingDays; i++)
            {
                var dt = lastOfMonth.AddDays(i);
                calendarDays.Add(new CalendarDayViewModel
                {
                    Date = DateOnly.FromDateTime(dt),
                    DayNumber = dt.Day,
                    IsCurrentMonth = false,
                    IsPast = true,
                    IsBooked = true,
                    IsBlocked = true
                });
            }
        }

        ViewBag.CalendarDays = calendarDays;
        ViewBag.MonthYear = now.ToString("MMMM yyyy");
        ViewBag.Month = now.Month;
        ViewBag.Year = now.Year;

        var viewModel = new ScanBookingResultViewModel
        {
            StudentID = student.ID,
            StudentName = student.FullName,
            NationalID = student.NationalID,
            CityName = cityName,
            DormitoryCityID = cityId,
            IsEligible = true,
            EligibilityMessage = "يمكن حجز الوجبات"
        };

        return View("ScanResult", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Meals.Book", "CanCreate")]
    public async Task<IActionResult> BookDates(BookDatesViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "بيانات غير صالحة";
            return RedirectToAction("Calendar", new { studentId = model.StudentID });
        }

        if (model.SelectedDates == null || model.SelectedDates.Count == 0)
        {
            TempData["Error"] = "الرجاء اختيار يوم واحد على الأقل";
            return RedirectToAction("Calendar", new { studentId = model.StudentID });
        }

        model.ScanMethod = "Manual";
        var (successCount, errors) = await _bookingService.BookDatesAsync(model, CurrentUserId);

        if (successCount > 0)
            TempData["Success"] = $"تم حجز {successCount} وجبة بنجاح";
        if (errors.Count > 0)
            TempData["Error"] = string.Join(" | ", errors.Take(3));

        return RedirectToAction("Calendar", new { studentId = model.StudentID });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Meals.Book", "CanCreate")]
    public async Task<IActionResult> Book(BookMealViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "بيانات غير صالحة";
            return RedirectToAction("Index");
        }

        var (success, message) = await _bookingService.BookMealAsync(model, CurrentUserId);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction("Index");
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

        return Json(new { success = true, cityId = allocation.CityRoom?.CityBuilding?.DormitoryCityID ?? 0 });
    }
}
