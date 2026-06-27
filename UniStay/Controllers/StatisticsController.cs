using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Statistics;

namespace UniStay.Controllers;

    [Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
public class StatisticsController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly IReportExportService _export;

    public StatisticsController(AssuitDbContext db, IReportExportService export)
    {
        _db = db;
        _export = export;
    }

    // ====================================================================
    // DASHBOARD
    // ====================================================================
    [HttpGet]
    [RequirePermission("Statistics.View", "CanView")]
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var vm = new StatisticsDashboardViewModel
        {
            TotalApplicants = await _db.Applications.CountAsync(),
            AcceptedApplicants = await _db.Applications.CountAsync(a => a.Status == "Accepted"),
            RejectedApplicants = await _db.Applications.CountAsync(a => a.Status == "Rejected"),
            PendingApplicants = await _db.Applications.CountAsync(a => a.Status == "Pending"),
            TotalAllocated = await _db.Allocations.CountAsync(a => a.Status == "Active"),
            TotalStudents = await _db.Students.CountAsync(s => s.IsActive == true && s.IsDeleted != true),
            PrintedCards = await _db.CardPrintQueues.CountAsync(q => q.Status == "Printed"),
            TodayConsumedMeals = await _db.MealConsumptions.CountAsync(m => m.MealDate == today),
            MonthlyApplications = await GetMonthlyApplications(null, null, null, null),
            ApplicationsByFaculty = await GetApplicationsByFaculty(null, null, null, null),
            ApplicationsByCity = await GetApplicationsByCity(null, null, null, null),
            OccupancyByCity = await GetOccupancyByCity(),
            MealConsumptionByType = await GetMealConsumptionByType(null, null, null)
        };

        return View(vm);
    }

    // ====================================================================
    // MODULE 1: APPLICANTS STATISTICS
    // ====================================================================
    [HttpGet]
    [RequirePermission("Statistics.View", "CanView")]
    public async Task<IActionResult> Applicants(string? academicYear, int? cityId, string? faculty, DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.Applications.AsQueryable();

        if (!string.IsNullOrEmpty(academicYear))
            query = query.Where(a => a.AcademicYear == academicYear);
        if (cityId.HasValue)
            query = query.Where(a => a.DormitoryCityID == cityId.Value);
        if (!string.IsNullOrEmpty(faculty))
            query = query.Where(a => a.Student!.Faculty == faculty);
        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value.AddDays(1));

        var vm = new ApplicantsStatisticsViewModel
        {
            Total = await query.CountAsync(),
            Accepted = await query.CountAsync(a => a.Status == "Accepted"),
            Rejected = await query.CountAsync(a => a.Status == "Rejected"),
            Pending = await query.CountAsync(a => a.Status == "Pending"),
            UnderReview = await query.CountAsync(a => a.Status == "UnderReview"),
            Returned = await query.CountAsync(a => a.Status == "Returned"),
            MonthlyApplications = await GetMonthlyApplications(academicYear, cityId, faculty, null),
            ApplicationsByFaculty = await GetApplicationsByFaculty(academicYear, cityId, faculty, null),
            ApplicationsByCity = await GetApplicationsByCity(academicYear, cityId, faculty, null),
            AcademicYears = await _db.Applications.Where(a => a.AcademicYear != null).Select(a => a.AcademicYear!).Distinct().OrderByDescending(y => y).ToListAsync(),
            Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted).Select(c => new FilterLookup { ID = c.ID, Name = c.Name }).ToListAsync(),
            Faculties = await _db.Faculties.Where(f => f.IsActive).Select(f => f.Name).OrderBy(f => f).ToListAsync(),
            FilterAcademicYear = academicYear,
            FilterCityId = cityId,
            FilterFaculty = faculty,
            FilterFromDate = fromDate,
            FilterToDate = toDate
        };

        vm.ApplicationsByStatus = new List<ChartDataPoint>
        {
            new() { Label = "مقبول", Value = vm.Accepted, Color = "#198754" },
            new() { Label = "مرفوض", Value = vm.Rejected, Color = "#dc3545" },
            new() { Label = "معلق", Value = vm.Pending, Color = "#ffc107" },
            new() { Label = "قيد المراجعة", Value = vm.UnderReview, Color = "#0dcaf0" },
            new() { Label = "معاد للتصحيح", Value = vm.Returned, Color = "#6c757d" }
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Statistics.View", "CanView")]
    public async Task<IActionResult> ApplicantsChartData(string? academicYear, int? cityId, string? faculty)
    {
        return Json(new ChartJsonResponse
        {
            Data = await GetMonthlyApplications(academicYear, cityId, faculty, null)
        });
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> ExportApplicantsExcel(string? academicYear, int? cityId, string? faculty, DateTime? fromDate, DateTime? toDate)
    {
        var data = await BuildApplicantsExportData(academicYear, cityId, faculty, fromDate, toDate);
        var columns = new[] { "م", "الاسم", "الرقم القومي", "الكلية", "المدينة", "الحالة", "تاريخ التقديم" };
        var bytes = _export.ExportToExcel("إحصائية المتقدمين", columns, data, r => new object?[] {
            r.Index, r.StudentName, r.NationalID, r.Faculty, r.CityName, r.Status, r.Date
        });
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ApplicantsStatistics.xlsx");
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> ExportApplicantsPdf(string? academicYear, int? cityId, string? faculty, DateTime? fromDate, DateTime? toDate)
    {
        var data = await BuildApplicantsExportData(academicYear, cityId, faculty, fromDate, toDate);
        var columns = new[] { "م", "الاسم", "الكلية", "الحالة", "تاريخ التقديم" };
        var rows = data.Select(r => new[] { r.Index.ToString(), r.StudentName ?? "", r.Faculty ?? "", r.Status ?? "", r.Date ?? "" }).ToArray();
        var pdf = _export.ExportToPdf("إحصائية المتقدمين", columns, rows);
        return File(pdf, "application/pdf", "ApplicantsStatistics.pdf");
    }

    // ====================================================================
    // MODULE 2: ALLOCATED STUDENTS STATISTICS
    // ====================================================================
    [HttpGet]
    [RequirePermission("Statistics.View", "CanView")]
    public async Task<IActionResult> AllocatedStudents(int? cityId, string? academicYear)
    {
        academicYear ??= GetCurrentAcademicYear();

        var query = _db.Allocations.Include(a => a.CityRoom).ThenInclude(r => r.CityBuilding).Include(a => a.CityRoom.CityBuilding.DormitoryCity)
            .Where(a => a.Status == "Active" && a.AcademicYear == academicYear).AsQueryable();

        if (cityId.HasValue)
            query = query.Where(a => a.CityRoom!.CityBuilding!.DormitoryCityID == cityId.Value);

        var allocated = await query.ToListAsync();

        var byCity = allocated.GroupBy(a => a.CityRoom?.CityBuilding?.DormitoryCity?.Name ?? "---")
            .Select(g => new AllocationCityStat
            {
                CityName = g.Key,
                Allocated = g.Count()
            }).ToList();

        var totalBeds = 0;
        var totalOccupied = 0;

        if (cityId.HasValue)
        {
            var rooms = await _db.CityRooms.Where(r => r.CityBuilding!.DormitoryCityID == cityId.Value && r.IsActive == true && r.IsDeleted != true).ToListAsync();
            totalBeds = rooms.Sum(r => (int)r.BedsCount);
            totalOccupied = rooms.Sum(r => (int)r.CurrentOccupancy);
        }
        else
        {
            var rooms = await _db.CityRooms.Where(r => r.IsActive == true && r.IsDeleted != true).ToListAsync();
            totalBeds = rooms.Sum(r => (int)r.BedsCount);
            totalOccupied = rooms.Sum(r => (int)r.CurrentOccupancy);
        }

        var byBuilding = allocated.GroupBy(a => new { a.CityRoom?.CityBuilding?.BuildingName, a.CityRoom?.CityBuildingID })
            .Select(g =>
            {
                var totalCap = _db.CityRooms.Where(r => r.CityBuildingID == g.Key.CityBuildingID).Sum(r => (int)r.BedsCount);
                return new AllocationBuildingStat
                {
                    BuildingName = g.Key.BuildingName ?? "---",
                    Allocated = g.Count(),
                    Capacity = totalCap,
                    OccupancyPercent = totalCap > 0 ? Math.Round((decimal)g.Count() / totalCap * 100, 1) : 0
                };
            }).ToList();

        var vm = new AllocatedStudentsStatisticsViewModel
        {
            TotalAllocated = allocated.Count,
            TotalBeds = totalBeds,
            OccupancyPercent = totalBeds > 0 ? Math.Round((decimal)totalOccupied / totalBeds * 100, 1) : 0,
            ByCity = byCity,
            ByBuilding = byBuilding,
            CityDistribution = byCity.Select(c => new ChartDataPoint { Label = c.CityName, Value = c.Allocated }).ToList(),
            Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted).Select(c => new FilterLookup { ID = c.ID, Name = c.Name }).ToListAsync(),
            FilterCityId = cityId,
            FilterAcademicYear = academicYear
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> ExportAllocatedExcel(int? cityId, string? academicYear)
    {
        academicYear ??= GetCurrentAcademicYear();
        var query = _db.Allocations.Include(a => a.Student).Include(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
            .Where(a => a.Status == "Active" && a.AcademicYear == academicYear).AsQueryable();
        if (cityId.HasValue)
            query = query.Where(a => a.CityRoom!.CityBuilding!.DormitoryCityID == cityId.Value);

        var data = await query.Select(a => new
        {
            a.Student!.FullName, a.Student.NationalID, a.Student.Faculty,
            Building = a.CityRoom!.CityBuilding!.BuildingName,
            Room = a.CityRoom.RoomNumber,
            a.BedNumber
        }).ToListAsync();

        var columns = new[] { "م", "الاسم", "الرقم القومي", "الكلية", "المبنى", "الغرفة", "السرير" };
        var excelData = data.Select((r, i) => new { Row = r, Index = i + 1 }).ToList();
        var bytes = _export.ExportToExcel("إحصائية المقيمين", columns, excelData, x => new object?[] {
            x.Index, x.Row.FullName, x.Row.NationalID, x.Row.Faculty, x.Row.Building, x.Row.Room, x.Row.BedNumber
        });
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AllocatedStudents.xlsx");
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> ExportAllocatedPdf(int? cityId, string? academicYear)
    {
        academicYear ??= GetCurrentAcademicYear();
        var query = _db.Allocations.Include(a => a.Student).Include(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
            .Where(a => a.Status == "Active" && a.AcademicYear == academicYear).AsQueryable();
        if (cityId.HasValue)
            query = query.Where(a => a.CityRoom!.CityBuilding!.DormitoryCityID == cityId.Value);

        var data = await query.Select(a => new
        {
            a.Student!.FullName, a.Student.Faculty,
            Building = a.CityRoom!.CityBuilding!.BuildingName,
            a.BedNumber
        }).ToListAsync();

        var columns = new[] { "م", "الاسم", "الكلية", "المبنى", "السرير" };
        var rows = data.Select((r, i) => new[] { (i + 1).ToString(), r.FullName, r.Faculty ?? "", r.Building, r.BedNumber.ToString() }).ToArray();
        var pdf = _export.ExportToPdf("إحصائية المقيمين", columns, rows);
        return File(pdf, "application/pdf", "AllocatedStudents.pdf");
    }

    // ====================================================================
    // MODULE 3: TOTAL STUDENTS STATISTICS
    // ====================================================================
    [HttpGet]
    [RequirePermission("Statistics.View", "CanView")]
    public async Task<IActionResult> TotalStudents()
    {
        var students = await _db.Students.Where(s => s.IsActive == true && s.IsDeleted != true).ToListAsync();

        var vm = new TotalStudentsStatisticsViewModel
        {
            TotalStudents = students.Count,
            MaleCount = students.Count(s => s.Gender == "Male"),
            FemaleCount = students.Count(s => s.Gender == "Female"),
            ActiveAllocations = await _db.Allocations.CountAsync(a => a.Status == "Active"),
            ByFaculty = students.Where(s => s.Faculty != null).GroupBy(s => s.Faculty!)
                .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
                .OrderByDescending(x => x.Value).Take(20).ToList(),
            ByAcademicYear = students.Where(s => s.AcademicYear != null).GroupBy(s => s.AcademicYear!.Value)
                .Select(g => new ChartDataPoint { Label = "سنة " + g.Key, Value = g.Count() })
                .OrderBy(x => x.Label).ToList(),
            ByGender = new List<ChartDataPoint>
            {
                new() { Label = "ذكور", Value = students.Count(s => s.Gender == "Male"), Color = "#0d6efd" },
                new() { Label = "إناث", Value = students.Count(s => s.Gender == "Female"), Color = "#d63384" }
            }
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> ExportTotalStudentsExcel()
    {
        var data = await _db.Students.Where(s => s.IsActive == true && s.IsDeleted != true)
            .Select(s => new { s.FullName, s.NationalID, s.Faculty, s.Gender, s.AcademicYear, s.Governorate })
            .ToListAsync();

        var columns = new[] { "م", "الاسم", "الرقم القومي", "الكلية", "النوع", "السنة الدراسية", "المحافظة" };
        var excelData = data.Select((r, i) => new { Row = r, Index = i + 1 }).ToList();
        var bytes = _export.ExportToExcel("إحصائية إجمالي الطلاب", columns, excelData, x => new object?[] {
            x.Index, x.Row.FullName, x.Row.NationalID, x.Row.Faculty, x.Row.Gender == "Male" ? "ذكر" : "أنثى", x.Row.AcademicYear, x.Row.Governorate
        });
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TotalStudents.xlsx");
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> ExportTotalStudentsPdf()
    {
        var data = await _db.Students.Where(s => s.IsActive == true && s.IsDeleted != true)
            .Select(s => new { s.FullName, s.Faculty, s.Gender, s.AcademicYear })
            .ToListAsync();
        var columns = new[] { "م", "الاسم", "الكلية", "النوع", "السنة" };
        var rows = data.Select((r, i) => new[] { (i + 1).ToString(), r.FullName, r.Faculty ?? "", r.Gender == "Male" ? "ذكر" : "أنثى", r.AcademicYear?.ToString() ?? "" }).ToArray();
        var pdf = _export.ExportToPdf("إحصائية إجمالي الطلاب", columns, rows);
        return File(pdf, "application/pdf", "TotalStudents.pdf");
    }

    // ====================================================================
    // MODULE 4: PRINTED CARDS STATISTICS
    // ====================================================================
    [HttpGet]
    [RequirePermission("Statistics.View", "CanView")]
    public async Task<IActionResult> PrintedCards(DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.CardPrintQueues.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(q => q.QueuedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(q => q.QueuedAt <= toDate.Value.AddDays(1));

        var vm = new PrintedCardsStatisticsViewModel
        {
            Total = await query.CountAsync(),
            Printed = await query.CountAsync(q => q.Status == "Printed"),
            Pending = await query.CountAsync(q => q.Status == "Pending"),
            Failed = await query.CountAsync(q => q.Status == "Failed"),
            DailyPrinting = await GetDailyPrinting(fromDate, toDate),
            MonthlyPrinting = await GetMonthlyPrinting(fromDate, toDate),
            FilterFromDate = fromDate,
            FilterToDate = toDate
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> ExportCardsExcel(DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.CardPrintQueues.Include(q => q.Student).AsQueryable();
        if (fromDate.HasValue) query = query.Where(q => q.QueuedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(q => q.QueuedAt <= toDate.Value.AddDays(1));

        var data = await query.Select(q => new { q.Student!.FullName, q.Student.NationalID, q.Status, q.QueuedAt, q.PrintedAt }).ToListAsync();
        var columns = new[] { "م", "الاسم", "الرقم القومي", "الحالة", "تاريخ الإدراج", "تاريخ الطباعة" };
        var excelData = data.Select((r, i) => new { Row = r, Index = i + 1 }).ToList();
        var bytes = _export.ExportToExcel("إحصائية البطاقات", columns, excelData, x => new object?[] {
            x.Index, x.Row.FullName, x.Row.NationalID, x.Row.Status, x.Row.QueuedAt?.ToString("yyyy/MM/dd"), x.Row.PrintedAt?.ToString("yyyy/MM/dd")
        });
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CardStatistics.xlsx");
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> ExportCardsPdf(DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.CardPrintQueues.Include(q => q.Student).AsQueryable();
        if (fromDate.HasValue) query = query.Where(q => q.QueuedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(q => q.QueuedAt <= toDate.Value.AddDays(1));

        var data = await query.Select(q => new { q.Student!.FullName, q.Status, q.QueuedAt }).ToListAsync();
        var columns = new[] { "م", "الاسم", "الحالة", "التاريخ" };
        var rows = data.Select((r, i) => new[] { (i + 1).ToString(), r.FullName, r.Status ?? "", r.QueuedAt?.ToString("yyyy/MM/dd") ?? "" }).ToArray();
        var pdf = _export.ExportToPdf("إحصائية البطاقات", columns, rows);
        return File(pdf, "application/pdf", "CardStatistics.pdf");
    }

    // ====================================================================
    // MODULE 5: MEAL CONSUMPTION STATISTICS
    // ====================================================================
    [HttpGet]
    [RequirePermission("Statistics.View", "CanView")]
    public async Task<IActionResult> MealConsumption(int? cityId, DateTime? date)
    {
        date ??= DateTime.UtcNow.Date;

        var query = _db.MealConsumptions.Include(m => m.Meal).AsQueryable();

        if (cityId.HasValue)
            query = query.Where(m => m.DormitoryCityID == cityId.Value);
        if (date.HasValue)
            query = query.Where(m => m.MealDate == DateOnly.FromDateTime(date.Value));

        var list = await query.ToListAsync();

        var mealTypeGroups = list.GroupBy(m => m.Meal?.MealType ?? "غير محدد")
            .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
            .ToList();

        var breakfastCount = mealTypeGroups.FirstOrDefault(m => m.Label == "Breakfast")?.Value ?? 0;
        var lunchCount = mealTypeGroups.FirstOrDefault(m => m.Label == "Lunch")?.Value ?? 0;
        var dinnerCount = mealTypeGroups.FirstOrDefault(m => m.Label == "Dinner")?.Value ?? 0;

        var vm = new MealConsumptionStatisticsViewModel
        {
            BreakfastCount = (int)breakfastCount,
            LunchCount = (int)lunchCount,
            DinnerCount = (int)dinnerCount,
            Total = list.Count,
            DailyConsumption = await GetDailyMealConsumption(cityId, date),
            ConsumptionByCity = await GetMealConsumptionByCity(date),
            ConsumptionByMealType = await GetMealConsumptionByType(cityId, date, null),
            Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted).Select(c => new FilterLookup { ID = c.ID, Name = c.Name }).ToListAsync(),
            FilterCityId = cityId,
            FilterDate = date
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> ExportMealsExcel(int? cityId, DateTime? date)
    {
        date ??= DateTime.UtcNow.Date;
        var query = _db.MealConsumptions.Include(m => m.Meal).Include(m => m.Student).AsQueryable();
        if (cityId.HasValue) query = query.Where(m => m.DormitoryCityID == cityId.Value);
        if (date.HasValue) query = query.Where(m => m.MealDate == DateOnly.FromDateTime(date.Value));

        var data = await query.Select(m => new { m.Student!.FullName, m.Meal!.MealType, m.MealDate, m.ConsumedAt }).ToListAsync();
        var columns = new[] { "م", "الاسم", "نوع الوجبة", "التاريخ", "الوقت" };
        var excelData = data.Select((r, i) => new { Row = r, Index = i + 1 }).ToList();
        var bytes = _export.ExportToExcel("إحصائية الوجبات", columns, excelData, x => new object?[] {
            x.Index, x.Row.FullName, x.Row.MealType, x.Row.MealDate.ToString(), x.Row.ConsumedAt?.ToString("HH:mm")
        });
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MealStatistics.xlsx");
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> ExportMealsPdf(int? cityId, DateTime? date)
    {
        date ??= DateTime.UtcNow.Date;
        var query = _db.MealConsumptions.Include(m => m.Meal).Include(m => m.Student).AsQueryable();
        if (cityId.HasValue) query = query.Where(m => m.DormitoryCityID == cityId.Value);
        if (date.HasValue) query = query.Where(m => m.MealDate == DateOnly.FromDateTime(date.Value));

        var data = await query.Select(m => new { m.Student!.FullName, m.Meal!.MealType, m.MealDate }).ToListAsync();
        var columns = new[] { "م", "الاسم", "الوجبة", "التاريخ" };
        var rows = data.Select((r, i) => new[] { (i + 1).ToString(), r.FullName, r.MealType, r.MealDate.ToString() }).ToArray();
        var pdf = _export.ExportToPdf("إحصائية الوجبات", columns, rows);
        return File(pdf, "application/pdf", "MealStatistics.pdf");
    }

    // ====================================================================
    // PRINT VIEW (unified print for dashboard)
    // ====================================================================
    [HttpGet]
    [RequirePermission("Statistics.View", "CanView")]
    public async Task<IActionResult> Print(int? cityId, string? academicYear)
    {
        var vm = new StatisticsDashboardViewModel
        {
            TotalApplicants = await _db.Applications.CountAsync(),
            AcceptedApplicants = await _db.Applications.CountAsync(a => a.Status == "Accepted"),
            RejectedApplicants = await _db.Applications.CountAsync(a => a.Status == "Rejected"),
            PendingApplicants = await _db.Applications.CountAsync(a => a.Status == "Pending"),
            TotalAllocated = await _db.Allocations.CountAsync(a => a.Status == "Active"),
            TotalStudents = await _db.Students.CountAsync(s => s.IsActive == true && s.IsDeleted != true),
            PrintedCards = await _db.CardPrintQueues.CountAsync(q => q.Status == "Printed"),
            TodayConsumedMeals = await _db.MealConsumptions.CountAsync(m => m.MealDate == DateOnly.FromDateTime(DateTime.UtcNow)),
            ApplicationsByStatus = new List<ChartDataPoint>
            {
                new() { Label = "مقبول", Value = await _db.Applications.CountAsync(a => a.Status == "Accepted"), Color = "#198754" },
                new() { Label = "مرفوض", Value = await _db.Applications.CountAsync(a => a.Status == "Rejected"), Color = "#dc3545" },
                new() { Label = "معلق", Value = await _db.Applications.CountAsync(a => a.Status == "Pending"), Color = "#ffc107" }
            },
            OccupancyByCity = await GetOccupancyByCity(),
            MealConsumptionByType = await GetMealConsumptionByType(null, null, null)
        };

        return View(vm);
    }

    // ====================================================================
    // PRIVATE HELPERS
    // ====================================================================
    private async Task<List<ChartDataPoint>> GetMonthlyApplications(string? academicYear, int? cityId, string? faculty, DateTime? toDate)
    {
        var query = _db.Applications.AsQueryable();
        if (!string.IsNullOrEmpty(academicYear)) query = query.Where(a => a.AcademicYear == academicYear);
        if (cityId.HasValue) query = query.Where(a => a.DormitoryCityID == cityId.Value);
        if (!string.IsNullOrEmpty(faculty)) query = query.Where(a => a.Student!.Faculty == faculty);
        if (toDate.HasValue) query = query.Where(a => a.CreatedAt <= toDate.Value.AddDays(1));

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        query = query.Where(a => a.CreatedAt >= sixMonthsAgo);

        var raw = await query
            .GroupBy(a => new { a.CreatedAt!.Value.Year, a.CreatedAt.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        return raw.Select(x => new ChartDataPoint
        {
            Label = $"{x.Year}/{x.Month:D2}",
            Value = x.Count
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetApplicationsByFaculty(string? academicYear, int? cityId, string? faculty, DateTime? toDate)
    {
        var query = _db.Applications.Include(a => a.Student).AsQueryable();
        if (!string.IsNullOrEmpty(academicYear)) query = query.Where(a => a.AcademicYear == academicYear);
        if (cityId.HasValue) query = query.Where(a => a.DormitoryCityID == cityId.Value);
        if (!string.IsNullOrEmpty(faculty)) query = query.Where(a => a.Student!.Faculty == faculty);
        if (toDate.HasValue) query = query.Where(a => a.CreatedAt <= toDate.Value.AddDays(1));

        var data = await query.Where(a => a.Student!.Faculty != null)
            .GroupBy(a => a.Student!.Faculty!)
            .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value).Take(15).ToListAsync();

        return data;
    }

    private async Task<List<ChartDataPoint>> GetApplicationsByCity(string? academicYear, int? cityId, string? faculty, DateTime? toDate)
    {
        var query = _db.Applications.Include(a => a.DormitoryCity).AsQueryable();
        if (!string.IsNullOrEmpty(academicYear)) query = query.Where(a => a.AcademicYear == academicYear);
        if (cityId.HasValue) query = query.Where(a => a.DormitoryCityID == cityId.Value);
        if (!string.IsNullOrEmpty(faculty)) query = query.Where(a => a.Student!.Faculty == faculty);
        if (toDate.HasValue) query = query.Where(a => a.CreatedAt <= toDate.Value.AddDays(1));

        var data = await query.Where(a => a.DormitoryCity != null)
            .GroupBy(a => a.DormitoryCity!.Name)
            .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value).ToListAsync();

        return data;
    }

    private async Task<List<ChartDataPoint>> GetOccupancyByCity()
    {
        var rooms = await _db.CityRooms.Where(r => r.IsActive == true && r.IsDeleted != true)
            .Include(r => r.CityBuilding).ThenInclude(b => b.DormitoryCity)
            .ToListAsync();

        var data = rooms.Where(r => r.CityBuilding?.DormitoryCity != null)
            .GroupBy(r => r.CityBuilding!.DormitoryCity!.Name)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = Math.Round((decimal)g.Sum(r => (int)r.CurrentOccupancy) / Math.Max(g.Sum(r => (int)r.BedsCount), 1) * 100, 1)
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        return data;
    }

    private async Task<List<ChartDataPoint>> GetMealConsumptionByType(int? cityId, DateTime? date, string? mealType)
    {
        var query = _db.MealConsumptions.Include(m => m.Meal).AsQueryable();
        if (cityId.HasValue) query = query.Where(m => m.DormitoryCityID == cityId.Value);
        if (date.HasValue) query = query.Where(m => m.MealDate == DateOnly.FromDateTime(date.Value));
        if (!string.IsNullOrEmpty(mealType)) query = query.Where(m => m.Meal!.MealType == mealType);

        var data = await query.Where(m => m.Meal != null)
            .GroupBy(m => m.Meal!.MealType)
            .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
            .ToListAsync();

        return data;
    }

    private async Task<List<ChartDataPoint>> GetDailyPrinting(DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.CardPrintQueues.Where(q => q.Status == "Printed").AsQueryable();
        if (fromDate.HasValue) query = query.Where(q => q.PrintedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(q => q.PrintedAt <= toDate.Value.AddDays(1));

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        query = query.Where(q => q.PrintedAt >= sevenDaysAgo);

        var raw = await query
            .GroupBy(q => q.PrintedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return raw.Select(x => new ChartDataPoint
        {
            Label = x.Date.ToString("MM/dd"),
            Value = x.Count
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetMonthlyPrinting(DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.CardPrintQueues.Where(q => q.Status == "Printed").AsQueryable();
        if (fromDate.HasValue) query = query.Where(q => q.PrintedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(q => q.PrintedAt <= toDate.Value.AddDays(1));

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        query = query.Where(q => q.PrintedAt >= sixMonthsAgo);

        var raw = await query
            .GroupBy(q => new { q.PrintedAt!.Value.Year, q.PrintedAt.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        return raw.Select(x => new ChartDataPoint
        {
            Label = $"{x.Year}/{x.Month:D2}",
            Value = x.Count
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetDailyMealConsumption(int? cityId, DateTime? date)
    {
        var query = _db.MealConsumptions.AsQueryable();
        if (cityId.HasValue) query = query.Where(m => m.DormitoryCityID == cityId.Value);
        if (date.HasValue) query = query.Where(m => m.MealDate == DateOnly.FromDateTime(date.Value));

        var sevenDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        query = query.Where(m => m.MealDate >= sevenDaysAgo);

        var raw = await query
            .GroupBy(m => m.MealDate)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return raw.Select(x => new ChartDataPoint
        {
            Label = x.Date.ToString("MM/dd"),
            Value = x.Count
        }).ToList();
    }

    private async Task<List<ChartDataPoint>> GetMealConsumptionByCity(DateTime? date)
    {
        var query = _db.MealConsumptions.Include(m => m.DormitoryCity).AsQueryable();
        if (date.HasValue) query = query.Where(m => m.MealDate == DateOnly.FromDateTime(date.Value));

        var data = await query.Where(m => m.DormitoryCity != null)
            .GroupBy(m => m.DormitoryCity!.Name)
            .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        return data;
    }

    private async Task<List<ExportRow>> BuildApplicantsExportData(string? academicYear, int? cityId, string? faculty, DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.Applications.Include(a => a.Student).Include(a => a.DormitoryCity).AsQueryable();
        if (!string.IsNullOrEmpty(academicYear)) query = query.Where(a => a.AcademicYear == academicYear);
        if (cityId.HasValue) query = query.Where(a => a.DormitoryCityID == cityId.Value);
        if (!string.IsNullOrEmpty(faculty)) query = query.Where(a => a.Student!.Faculty == faculty);
        if (fromDate.HasValue) query = query.Where(a => a.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(a => a.CreatedAt <= toDate.Value.AddDays(1));

        var list = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        return list.Select((a, i) => new ExportRow
        {
            Index = i + 1,
            StudentName = a.Student?.FullName ?? "",
            NationalID = a.Student?.NationalID ?? "",
            Faculty = a.Student?.Faculty,
            CityName = a.DormitoryCity?.Name,
            Status = a.Status,
            Date = a.CreatedAt?.ToString("yyyy/MM/dd") ?? ""
        }).ToList();
    }

    // ====================================================================
    // MODULE 6: SMS STATISTICS
    // ====================================================================
    [HttpGet]
    [RequirePermission("Statistics.View", "CanView")]
    public async Task<IActionResult> SmsStatistics(DateTime? fromDate, DateTime? toDate, string? status, string? type, int page = 1)
    {
        const int pageSize = 30;

        var query = _db.EmailLogs.Include(s => s.Student).AsQueryable();

        if (fromDate.HasValue) query = query.Where(s => s.SentAt >= fromDate.Value || s.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(s => s.SentAt <= toDate.Value.AddDays(1) || s.CreatedAt <= toDate.Value.AddDays(1));
        if (!string.IsNullOrEmpty(status))
        {
            if (status == "Sent") query = query.Where(s => s.Status == "Sent" || s.Status == "Success");
            else if (status == "Failed") query = query.Where(s => s.Status == "Failed" || s.Status == "Error");
            else if (status == "Pending") query = query.Where(s => s.Status == "Pending" || s.Status == null);
        }
        if (!string.IsNullOrEmpty(type))
            query = query.Where(s => s.EmailType == type);

        // Summary stats
        var allLogs = await query.ToListAsync();
        var totalSent = allLogs.Count(s => s.Status == "Sent" || s.Status == "Success");
        var totalFailed = allLogs.Count(s => s.Status == "Failed" || s.Status == "Error");
        var totalPending = allLogs.Count(s => s.Status == "Pending" || s.Status == null);
        var total = allLogs.Count;

        // Charts
        var dailySms = allLogs
            .GroupBy(s => (s.SentAt ?? s.CreatedAt ?? DateTime.MinValue).Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint { Label = g.Key.ToString("yyyy-MM-dd"), Value = g.Count() })
            .ToList();

        var smsByType = allLogs
            .GroupBy(s => s.EmailType ?? "أخرى")
            .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Count() })
            .ToList();

        var smsByStatus = new List<ChartDataPoint>
        {
            new() { Label = "تم الإرسال", Value = totalSent, Color = "#00c853" },
            new() { Label = "فشل", Value = totalFailed, Color = "#ff1744" },
            new() { Label = "قيد الانتظار", Value = totalPending, Color = "#ffd600" }
        };

        // Paginated logs
        var logs = allLogs.OrderByDescending(s => s.SentAt ?? s.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new SmsLogRowViewModel
            {
                ID = s.ID,
                RecipientName = s.Student?.FullName ?? s.RecipientEmail,
                PhoneNumber = s.RecipientEmail,
                MessageType = s.EmailType ?? "",
                MessageTypeDisplay = s.EmailType ?? "",
                Status = s.Status ?? "Pending",
                StatusDisplay = s.Status == "Sent" || s.Status == "Success" ? "تم الإرسال" : s.Status == "Failed" || s.Status == "Error" ? "فشل" : "قيد الانتظار",
                MessageContent = s.Subject,
                SentAt = s.SentAt ?? s.CreatedAt
            }).ToList();

        var vm = new SmsStatisticsViewModel
        {
            TotalSent = totalSent,
            TotalDelivered = totalSent,
            TotalFailed = totalFailed,
            TotalPendingSms = totalPending,
            DailySmsSent = dailySms,
            SmsByType = smsByType,
            SmsByStatus = smsByStatus,
            RecentLogs = logs,
            FilterFromDate = fromDate,
            FilterToDate = toDate,
            FilterStatus = status,
            FilterType = type,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            TotalLogs = total
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> SmsStatisticsExportExcel(DateTime? fromDate, DateTime? toDate, string? status, string? type)
    {
        var query = _db.EmailLogs.Include(s => s.Student).AsQueryable();
        if (fromDate.HasValue) query = query.Where(s => s.SentAt >= fromDate.Value || s.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(s => s.SentAt <= toDate.Value.AddDays(1) || s.CreatedAt <= toDate.Value.AddDays(1));
        if (!string.IsNullOrEmpty(status))
        {
            if (status == "Sent") query = query.Where(s => s.Status == "Sent" || s.Status == "Success");
            else if (status == "Failed") query = query.Where(s => s.Status == "Failed" || s.Status == "Error");
            else if (status == "Pending") query = query.Where(s => s.Status == "Pending" || s.Status == null);
        }
        if (!string.IsNullOrEmpty(type)) query = query.Where(s => s.EmailType == type);

        var logs = await query.OrderByDescending(s => s.SentAt ?? s.CreatedAt).ToListAsync();
        var rows = logs.Select((s, idx) => new { s, idx }).ToList();
        var columns = new[] { "#", "المستلم", "جهة الاتصال", "نوع الرسالة", "الحالة", "المحتوى", "تاريخ الإرسال" };
        var data = _export.ExportToExcel("إحصائية الرسائل", columns, rows, r => new object?[] {
            r.idx + 1, r.s.Student?.FullName ?? r.s.RecipientEmail, r.s.RecipientEmail, r.s.EmailType ?? "",
            r.s.Status == "Sent" || r.s.Status == "Success" ? "تم الإرسال" : r.s.Status == "Failed" || r.s.Status == "Error" ? "فشل" : "قيد الانتظار",
            r.s.Subject, (r.s.SentAt ?? r.s.CreatedAt)?.ToString("yyyy-MM-dd HH:mm")
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SmsStatistics.xlsx");
    }

    [HttpGet]
    [RequirePermission("Statistics.Export", "CanView")]
    public async Task<IActionResult> SmsStatisticsExportPdf(DateTime? fromDate, DateTime? toDate, string? status, string? type)
    {
        var query = _db.EmailLogs.Include(s => s.Student).AsQueryable();
        if (fromDate.HasValue) query = query.Where(s => s.SentAt >= fromDate.Value || s.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(s => s.SentAt <= toDate.Value.AddDays(1) || s.CreatedAt <= toDate.Value.AddDays(1));
        if (!string.IsNullOrEmpty(status))
        {
            if (status == "Sent") query = query.Where(s => s.Status == "Sent" || s.Status == "Success");
            else if (status == "Failed") query = query.Where(s => s.Status == "Failed" || s.Status == "Error");
            else if (status == "Pending") query = query.Where(s => s.Status == "Pending" || s.Status == null);
        }
        if (!string.IsNullOrEmpty(type)) query = query.Where(s => s.EmailType == type);

        var logs = await query.OrderByDescending(s => s.SentAt ?? s.CreatedAt).ToListAsync();
        var columns = new[] { "المستلم", "النوع", "الحالة", "التاريخ" };
        var pdfData = logs.Select(s => new[] {
            s.Student?.FullName ?? s.RecipientEmail, s.EmailType ?? "",
            s.Status == "Sent" || s.Status == "Success" ? "تم" : s.Status == "Failed" || s.Status == "Error" ? "فشل" : "معلق",
            (s.SentAt ?? s.CreatedAt)?.ToString("yyyy-MM-dd") ?? ""
        }).ToArray();
        var pdf = _export.ExportToPdf("إحصائية الرسائل القصيرة", columns, pdfData);
        return File(pdf, "application/pdf", "SmsStatistics.pdf");
    }

    // ====================================================================
    // CUSTOM REPORT BUILDER
    // ====================================================================
    [HttpGet]
    [RequirePermission("Statistics.View", "CanView")]
    public async Task<IActionResult> CustomReport(
        string reportType = "Students",
        string[]? selectedColumns = null,
        int? filterCityId = null,
        string? filterAcademicYear = null,
        string? filterStatus = null,
        string? filterFaculty = null,
        string? filterGender = null,
        string? filterGovernorate = null,
        DateTime? filterFromDate = null,
        DateTime? filterToDate = null,
        string? searchTerm = null)
    {
        var vm = new CustomReportViewModel
        {
            ReportType = reportType,
            FilterCityId = filterCityId,
            FilterAcademicYear = filterAcademicYear,
            FilterStatus = filterStatus,
            FilterFaculty = filterFaculty,
            FilterGender = filterGender,
            FilterGovernorate = filterGovernorate,
            FilterFromDate = filterFromDate,
            FilterToDate = filterToDate,
            SearchTerm = searchTerm,
            Cities = await _db.DormitoryCities.Where(c => !c.IsDeleted).Select(c => new FilterLookup { ID = c.ID, Name = c.Name }).ToListAsync(),
            AcademicYears = await _db.Applications.Select(a => a.AcademicYear).Distinct().OrderByDescending(a => a).ToListAsync(),
            Faculties = await _db.Students.Where(s => s.Faculty != null).Select(s => s.Faculty!).Distinct().OrderBy(f => f).ToListAsync(),
            Governorates = await _db.Students.Where(s => s.Governorate != null).Select(s => s.Governorate!).Distinct().OrderBy(g => g).ToListAsync()
        };

        if (!ReportTypeInfo.Types.ContainsKey(reportType))
        {
            vm.HasRun = true;
            return View(vm);
        }

        var def = ReportTypeInfo.Types[reportType];
        var cols = selectedColumns?.Length > 0 ? selectedColumns.ToList() : def.AvailableColumns.Keys.ToList();
        vm.Columns = cols;

        switch (reportType)
        {
            case "Students":
                await BuildStudentReport(vm);
                break;
            case "Applications":
                await BuildApplicationReport(vm);
                break;
            case "Allocations":
                await BuildAllocationReport(vm);
                break;
            case "Violations":
                await BuildViolationReport(vm);
                break;
            case "Penalties":
                await BuildPenaltyReport(vm);
                break;
            case "Payments":
                await BuildPaymentReport(vm);
                break;
        }

        vm.HasRun = true;
        return View(vm);
    }

    private async Task BuildStudentReport(CustomReportViewModel vm)
    {
        var query = _db.Students.Where(s => s.IsDeleted != true).AsQueryable();

        if (vm.FilterCityId.HasValue)
            query = query.Where(s => s.Applications.Any(a => a.DormitoryCityID == vm.FilterCityId.Value));
        if (!string.IsNullOrEmpty(vm.FilterFaculty))
            query = query.Where(s => s.Faculty == vm.FilterFaculty);
        if (!string.IsNullOrEmpty(vm.FilterGovernorate))
            query = query.Where(s => s.Governorate == vm.FilterGovernorate);
        if (!string.IsNullOrEmpty(vm.FilterGender))
            query = query.Where(s => s.Gender == vm.FilterGender);
        if (!string.IsNullOrEmpty(vm.SearchTerm))
            query = query.Where(s => s.FullName.Contains(vm.SearchTerm) || (s.StudentCode != null && s.StudentCode.Contains(vm.SearchTerm)));

        var data = await query.OrderByDescending(s => s.CreatedAt).Take(5000).ToListAsync();
        vm.TotalCount = data.Count;

        foreach (var s in data)
        {
            var row = new Dictionary<string, string>();
            foreach (var col in vm.Columns)
            {
                row[col] = col switch
                {
                    "FullName" => s.FullName,
                    "NationalID" => s.NationalID ?? "",
                    "StudentCode" => s.StudentCode ?? "",
                    "Gender" => s.Gender == "Male" ? "ذكر" : "أنثى",
                    "Phone" => s.Phone,
                    "Email" => s.Email,
                    "Faculty" => s.Faculty ?? "",
                    "Department" => s.Department ?? "",
                    "GradeText" => s.GradeText ?? "",
                    "GradePercentage" => s.GradePercentage?.ToString("0.##") ?? "",
                    "Governorate" => s.Governorate ?? "",
                    "Markaz" => s.Markaz ?? "",
                    "City" => s.City ?? "",
                    "Address" => s.Address ?? "",
                    "DistanceFromUniv" => s.DistanceFromUniv?.ToString("0.##") ?? "",
                    "IsActive" => s.IsActive == true ? "نعم" : "لا",
                    "CreatedAt" => s.CreatedAt?.ToString("yyyy-MM-dd") ?? "",
                    _ => ""
                };
            }
            vm.Rows.Add(row);
        }
    }

    private async Task BuildApplicationReport(CustomReportViewModel vm)
    {
        var query = _db.Applications.Include(a => a.Student).Include(a => a.DormitoryCity).AsQueryable();

        if (vm.FilterCityId.HasValue)
            query = query.Where(a => a.DormitoryCityID == vm.FilterCityId.Value);
        if (!string.IsNullOrEmpty(vm.FilterAcademicYear))
            query = query.Where(a => a.AcademicYear == vm.FilterAcademicYear);
        if (!string.IsNullOrEmpty(vm.FilterFaculty))
            query = query.Where(a => a.Student.Faculty == vm.FilterFaculty);
        if (vm.FilterFromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= vm.FilterFromDate.Value);
        if (vm.FilterToDate.HasValue)
            query = query.Where(a => a.CreatedAt <= vm.FilterToDate.Value);
        if (!string.IsNullOrEmpty(vm.SearchTerm))
            query = query.Where(a => a.Student.FullName.Contains(vm.SearchTerm));

        var data = await query.OrderByDescending(a => a.CreatedAt).Take(5000).ToListAsync();
        vm.TotalCount = data.Count;

        foreach (var a in data)
        {
            var row = new Dictionary<string, string>();
            foreach (var col in vm.Columns)
            {
                row[col] = col switch
                {
                    "StudentName" => a.Student.FullName,
                    "NationalID" => a.Student.NationalID ?? "",
                    "DormitoryCity" => a.DormitoryCity.Name,
                    "AcademicYear" => a.AcademicYear,
                    "StudentType" => a.StudentType,
                    "HousingType" => a.HousingType,
                    "Status" => a.Status,
                    "CoordinationScore" => a.CoordinationScore?.ToString("0.##") ?? "",
                    "CreatedAt" => a.CreatedAt?.ToString("yyyy-MM-dd") ?? "",
                    "ReviewedAt" => a.ReviewedAt?.ToString("yyyy-MM-dd") ?? "",
                    _ => ""
                };
            }
            vm.Rows.Add(row);
        }
    }

    private async Task BuildAllocationReport(CustomReportViewModel vm)
    {
        var query = _db.Allocations
            .Include(a => a.Student).Include(a => a.CityRoom).ThenInclude(r => r.CityBuilding).ThenInclude(b => b.DormitoryCity)
            .AsQueryable();

        if (vm.FilterCityId.HasValue)
            query = query.Where(a => a.CityRoom.CityBuilding.DormitoryCityID == vm.FilterCityId.Value);
        if (!string.IsNullOrEmpty(vm.FilterAcademicYear))
            query = query.Where(a => a.AcademicYear == vm.FilterAcademicYear);
        if (!string.IsNullOrEmpty(vm.FilterFaculty))
            query = query.Where(a => a.Student.Faculty == vm.FilterFaculty);
        if (!string.IsNullOrEmpty(vm.SearchTerm))
            query = query.Where(a => a.Student.FullName.Contains(vm.SearchTerm));

        var data = await query.OrderByDescending(a => a.AllocatedAt).Take(5000).ToListAsync();
        vm.TotalCount = data.Count;

        foreach (var a in data)
        {
            var row = new Dictionary<string, string>();
            foreach (var col in vm.Columns)
            {
                row[col] = col switch
                {
                    "StudentName" => a.Student.FullName,
                    "NationalID" => a.Student.NationalID ?? "",
                    "DormitoryCity" => a.CityRoom.CityBuilding.DormitoryCity.Name,
                    "Building" => a.CityRoom.CityBuilding.BuildingName,
                    "RoomNumber" => a.CityRoom.RoomNumber,
                    "BedNumber" => a.BedNumber.ToString(),
                    "AcademicYear" => a.AcademicYear,
                    "Status" => a.Status,
                    "StartDate" => a.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    "EndDate" => a.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    _ => ""
                };
            }
            vm.Rows.Add(row);
        }
    }

    private async Task BuildViolationReport(CustomReportViewModel vm)
    {
        var query = _db.Violations.Include(v => v.Student).AsQueryable();

        if (vm.FilterCityId.HasValue)
            query = query.Where(v => v.DormitoryCityID == vm.FilterCityId.Value);
        if (!string.IsNullOrEmpty(vm.SearchTerm))
            query = query.Where(v => v.Student.FullName.Contains(vm.SearchTerm));

        var data = await query.OrderByDescending(v => v.RecordedAt).Take(5000).ToListAsync();
        vm.TotalCount = data.Count;

        foreach (var v in data)
        {
            var row = new Dictionary<string, string>();
            foreach (var col in vm.Columns)
            {
                row[col] = col switch
                {
                    "StudentName" => v.Student.FullName,
                    "ViolationType" => v.ViolationType,
                    "Description" => v.Description ?? "",
                    "Severity" => v.Severity,
                    "Status" => v.Status,
                    "FineAmount" => v.FineAmount?.ToString("0.##") ?? "",
                    "FinePaid" => v.FinePaid?.ToString("0.##") ?? "",
                    "RecordedAt" => v.RecordedAt?.ToString("yyyy-MM-dd") ?? "",
                    _ => ""
                };
            }
            vm.Rows.Add(row);
        }
    }

    private async Task BuildPenaltyReport(CustomReportViewModel vm)
    {
        var query = _db.StudentPenalties
            .Include(p => p.Student).Include(p => p.PenaltyType)
            .AsQueryable();

        if (!string.IsNullOrEmpty(vm.SearchTerm))
            query = query.Where(p => p.Student.FullName.Contains(vm.SearchTerm));

        var data = await query.OrderByDescending(p => p.RecordedAt).Take(5000).ToListAsync();
        vm.TotalCount = data.Count;

        foreach (var p in data)
        {
            var row = new Dictionary<string, string>();
            foreach (var col in vm.Columns)
            {
                row[col] = col switch
                {
                    "StudentName" => p.Student.FullName,
                    "PenaltyType" => p.PenaltyType.Name,
                    "FineAmount" => p.FineAmount?.ToString("0.##") ?? "",
                    "FinePaid" => p.FinePaid?.ToString("0.##") ?? "",
                    "Status" => p.Status,
                    "Description" => p.Description ?? "",
                    "RecordedAt" => p.RecordedAt?.ToString("yyyy-MM-dd") ?? "",
                    "ResolvedAt" => p.ResolvedAt?.ToString("yyyy-MM-dd") ?? "",
                    _ => ""
                };
            }
            vm.Rows.Add(row);
        }
    }

    private async Task BuildPaymentReport(CustomReportViewModel vm)
    {
        var query = _db.Payments.Include(p => p.Student).AsQueryable();

        if (vm.FilterFromDate.HasValue)
            query = query.Where(p => p.RecordedAt >= vm.FilterFromDate.Value);
        if (vm.FilterToDate.HasValue)
            query = query.Where(p => p.RecordedAt <= vm.FilterToDate.Value);
        if (!string.IsNullOrEmpty(vm.SearchTerm))
            query = query.Where(p => p.Student.FullName.Contains(vm.SearchTerm));

        var data = await query.OrderByDescending(p => p.RecordedAt).Take(5000).ToListAsync();
        vm.TotalCount = data.Count;

        foreach (var p in data)
        {
            var row = new Dictionary<string, string>();
            foreach (var col in vm.Columns)
            {
                row[col] = col switch
                {
                    "StudentName" => p.Student.FullName,
                    "PaymentType" => p.PaymentType,
                    "Amount" => p.Amount.ToString("0.##"),
                    "PaidAmount" => p.PaidAmount.ToString("0.##"),
                    "Status" => p.Status,
                    "MonthYear" => p.MonthYear ?? "",
                    "RecordedAt" => p.RecordedAt?.ToString("yyyy-MM-dd") ?? "",
                    "PaidAt" => p.PaidAt?.ToString("yyyy-MM-dd") ?? "",
                    _ => ""
                };
            }
            vm.Rows.Add(row);
        }
    }

    private static string GetCurrentAcademicYear()
    {
        var year = DateTime.Now.Year;
        return DateTime.Now.Month >= 6 ? $"{year}-{year + 1}" : $"{year - 1}-{year}";
    }

    private class ExportRow
    {
        public int Index { get; set; }
        public string StudentName { get; set; } = "";
        public string NationalID { get; set; } = "";
        public string? Faculty { get; set; }
        public string? CityName { get; set; }
        public string? Status { get; set; }
        public string Date { get; set; } = "";
    }
}
