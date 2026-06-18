using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Meal;

namespace UniStay.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class MealController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;
        private readonly IMealService _mealService;
        private readonly IReportExportService _export;

        public MealController(AssuitDbContext db, IAuditService audit, IEmailService email, IMealService mealService, IReportExportService export)
        {
            _db = db;
            _audit = audit;
            _email = email;
            _mealService = mealService;
            _export = export;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        [HttpGet]
        public async Task<IActionResult> Index(int? cityId, DateOnly? date)
        {
            var today = date ?? DateOnly.FromDateTime(DateTime.Today);

            var cities = await _db.DormitoryCities
                .Where(c => c.IsActive)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name })
                .ToListAsync();

            var model = new MealIndexViewModel
            {
                Cities = cities,
                SelectedDate = today,
                DormitoryCityID = cityId ?? 0
            };

            if (cityId.HasValue)
            {
                var meals = await _db.Meals
                    .Include(m => m.Student)
                    .Where(m => m.DormitoryCityID == cityId && m.MealDate == today)
                    .ToListAsync();

                model.TotalMeals = meals.Count;
                model.ConsumedCount = meals.Count(m => m.IsConsumed == true);
                model.CancelledCount = meals.Count(m => m.IsActive == false);
                model.BlockedCount = await _db.MealBlocks
                    .CountAsync(b => b.DormitoryCityID == cityId && b.FromDate <= today && b.ToDate >= today && b.IsActive == true);

                model.Meals = meals.Select(m => new MealRowViewModel
                {
                    ID = m.ID,
                    StudentID = m.StudentID,
                    StudentName = m.Student?.FullName ?? "",
                    NationalID = m.Student?.NationalID ?? "",
                    MealType = m.MealType,
                    Price = m.Price,
                    IsBooked = m.IsBooked ?? false,
                    IsConsumed = m.IsConsumed ?? false,
                    IsActive = m.IsActive ?? false,
                    CancelReason = m.CancelReason
                }).ToList();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMeals(int dormitoryCityId)
        {
            var city = await _db.DormitoryCities.FindAsync(dormitoryCityId);
            if (city == null) return NotFound();

            await _mealService.GenerateDailyMealsAsync(dormitoryCityId, DateTime.Today);

            await _audit.LogAsync(CurrentUserId, "Staff", "Meal.Generate", "Meal",
                null, null, new { dormitoryCityId, Date = DateTime.Today.ToString("yyyy-MM-dd") });

            TempData["Success"] = "تم توليد الوجبات بنجاح";
            return RedirectToAction("Index", new { cityId = dormitoryCityId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelIndividual(CancelIndividualViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صالحة" });

            var meals = await _db.Meals
                .Where(m => m.StudentID == model.StudentID
                    && m.DormitoryCityID == model.DormitoryCityID
                    && m.MealDate >= model.FromDate
                    && m.MealDate <= model.ToDate
                    && m.IsActive == true)
                .ToListAsync();

            if (!meals.Any())
                return Json(new { success = false, message = "لا توجد وجبات نشطة في هذا النطاق" });

            foreach (var meal in meals)
            {
                meal.IsActive = false;
                meal.CancelReason = model.Reason;
            }

            var cancellation = new MealCancellation
            {
                StudentID = model.StudentID,
                DormitoryCityID = model.DormitoryCityID,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                CancellationType = "Individual",
                CreatedBy = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };
            _db.MealCancellations.Add(cancellation);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Meal.CancelIndividual", "Meal",
                null, new { count = meals.Count },
                new { model.StudentID, model.FromDate, model.ToDate, model.Reason });

            return Json(new { success = true, message = $"تم إلغاء {meals.Count} وجبات" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBulk(CancelBulkViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صالحة" });

            await _mealService.CancelBulkMealsAsync(model.DormitoryCityID,
                model.FromDate.ToDateTime(TimeOnly.MinValue),
                model.ToDate.ToDateTime(TimeOnly.MinValue),
                model.Reason ?? "");

            var cancellation = new MealCancellation
            {
                DormitoryCityID = model.DormitoryCityID,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                CancellationType = "Bulk",
                CreatedBy = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };
            _db.MealCancellations.Add(cancellation);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Meal.CancelBulk", "MealCancellation",
                cancellation.ID, null, new { model.DormitoryCityID, model.FromDate, model.ToDate });

            return Json(new { success = true, message = "تم إلغاء الوجبات بنجاح" });
        }

        [HttpGet]
        public async Task<IActionResult> RamadanSchedule(int? cityId)
        {
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name })
                .ToListAsync();

            var model = new MealScheduleViewModel
            {
                MealType = "Ramadan",
                ViewTitle = "جدول وجبات رمضان",
                DormitoryCityID = cityId ?? 0
            };

            if (cityId.HasValue)
            {
                model.Schedules = await _db.MealSchedules
                    .Where(s => s.DormitoryCityID == cityId && s.MealType == "Ramadan")
                    .OrderBy(s => s.ScheduleDate)
                    .Select(s => new ScheduleRowViewModel
                    {
                        ID = s.ID,
                        ScheduleDate = s.ScheduleDate,
                        Description = s.Description,
                        SpecialPrice = s.SpecialPrice,
                        IsActive = s.IsActive ?? false
                    })
                    .ToListAsync();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RamadanSchedule(MealScheduleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صالحة";
                return RedirectToAction("RamadanSchedule", new { cityId = model.DormitoryCityID });
            }

            var schedule = new MealSchedule
            {
                DormitoryCityID = model.DormitoryCityID,
                ScheduleDate = model.ScheduleDate,
                MealType = "Ramadan",
                Description = model.Description,
                SpecialPrice = model.SpecialPrice,
                IsActive = true
            };

            _db.MealSchedules.Add(schedule);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Meal.RamadanSchedule", "MealSchedule",
                schedule.ID, null, new { schedule.DormitoryCityID, schedule.ScheduleDate, schedule.MealType });

            TempData["Success"] = "تمت إضافة الموعد بنجاح";
            return RedirectToAction("RamadanSchedule", new { cityId = model.DormitoryCityID });
        }

        [HttpGet]
        public async Task<IActionResult> ChristianSchedule(int? cityId)
        {
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name })
                .ToListAsync();

            var model = new MealScheduleViewModel
            {
                MealType = "Christian",
                ViewTitle = "جدول وجبات المسيحيين",
                DormitoryCityID = cityId ?? 0
            };

            if (cityId.HasValue)
            {
                model.Schedules = await _db.MealSchedules
                    .Where(s => s.DormitoryCityID == cityId && s.MealType == "Christian")
                    .OrderBy(s => s.ScheduleDate)
                    .Select(s => new ScheduleRowViewModel
                    {
                        ID = s.ID,
                        ScheduleDate = s.ScheduleDate,
                        Description = s.Description,
                        SpecialPrice = s.SpecialPrice,
                        IsActive = s.IsActive ?? false
                    })
                    .ToListAsync();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChristianSchedule(MealScheduleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صالحة";
                return RedirectToAction("ChristianSchedule", new { cityId = model.DormitoryCityID });
            }

            var schedule = new MealSchedule
            {
                DormitoryCityID = model.DormitoryCityID,
                ScheduleDate = model.ScheduleDate,
                MealType = "Christian",
                Description = model.Description,
                SpecialPrice = model.SpecialPrice,
                IsActive = true
            };

            _db.MealSchedules.Add(schedule);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Meal.ChristianSchedule", "MealSchedule",
                schedule.ID, null, new { schedule.DormitoryCityID, schedule.ScheduleDate, schedule.MealType });

            TempData["Success"] = "تمت إضافة الموعد بنجاح";
            return RedirectToAction("ChristianSchedule", new { cityId = model.DormitoryCityID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleScheduleActive(int id)
        {
            var schedule = await _db.MealSchedules.FindAsync(id);
            if (schedule == null)
                return Json(new { success = false, message = "الموعد غير موجود" });

            schedule.IsActive = !(schedule.IsActive ?? false);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Meal.ToggleSchedule", "MealSchedule",
                id, null, new { IsActive = schedule.IsActive });

            return Json(new { success = true, message = "تم تغيير الحالة" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var schedule = await _db.MealSchedules.FindAsync(id);
            if (schedule == null)
                return Json(new { success = false, message = "الموعد غير موجود" });

            _db.MealSchedules.Remove(schedule);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Meal.DeleteSchedule", "MealSchedule",
                id, null, null);

            return Json(new { success = true, message = "تم حذف الموعد" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Block(BlockStudentViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صالحة" });

            var student = await _db.Students.FindAsync(model.StudentID);
            if (student == null)
                return Json(new { success = false, message = "الطالب غير موجود" });

            var block = new MealBlock
            {
                StudentID = model.StudentID,
                DormitoryCityID = model.DormitoryCityID,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                Reason = model.Reason,
                IsActive = true,
                CreatedBy = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.MealBlocks.Add(block);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Meal.Block", "MealBlock",
                block.ID, null, new { model.StudentID, model.DormitoryCityID, model.FromDate, model.ToDate });

            return Json(new { success = true, message = "تم حظر الوجبات للطالب" });
        }

        [HttpGet]
        public async Task<IActionResult> Consume(int? studentId, string? searchTerm)
        {
            var model = new ConsumeViewModel
            {
                StudentID = studentId,
                SearchTerm = searchTerm
            };

            if (studentId.HasValue || !string.IsNullOrEmpty(searchTerm))
            {
                Student? student = null;
                if (studentId.HasValue)
                    student = await _db.Students.FirstOrDefaultAsync(s => s.ID == studentId.Value);
                else if (!string.IsNullOrEmpty(searchTerm))
                    student = await _db.Students.FirstOrDefaultAsync(s => s.NationalID == searchTerm);

                if (student != null)
                {
                    model.StudentID = student.ID;
                    model.SearchTerm = searchTerm;

                    var today = DateOnly.FromDateTime(DateTime.Today);
                    model.AvailableMeals = await _db.Meals
                        .Include(m => m.Student)
                        .Where(m => m.StudentID == student.ID && m.MealDate == today
                            && m.IsBooked == true && m.IsConsumed != true && m.IsActive == true)
                        .Select(m => new MealRowViewModel
                        {
                            ID = m.ID,
                            StudentID = m.StudentID,
                            StudentName = m.Student.FullName,
                            NationalID = m.Student.NationalID,
                            MealType = m.MealType,
                            Price = m.Price,
                            IsBooked = m.IsBooked ?? false,
                            IsConsumed = m.IsConsumed ?? false,
                            IsActive = m.IsActive ?? false
                        })
                        .ToListAsync();
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordConsumption(RecordConsumptionViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صالحة" });

            var meal = await _db.Meals
                .Include(m => m.Student)
                .FirstOrDefaultAsync(m => m.ID == model.MealID);

            if (meal == null)
                return Json(new { success = false, message = "الوجبة غير موجودة" });

            if (meal.IsConsumed == true)
                return Json(new { success = false, message = "الوجبة مستهلكة بالفعل" });

            if (meal.StudentID != model.StudentID)
                return Json(new { success = false, message = "الوجبة لا تخص هذا الطالب" });

            var canConsume = await _mealService.CanConsumeAsync(model.StudentID, meal.DormitoryCityID, DateTime.Today);
            if (!canConsume)
                return Json(new { success = false, message = "لا يمكن استهلاك الوجبة (الطالب محظور أو ملغي)" });

            var consumption = new MealConsumption
            {
                StudentID = model.StudentID,
                MealID = model.MealID,
                DormitoryCityID = meal.DormitoryCityID,
                MealDate = meal.MealDate,
                ScanMethod = model.ScanMethod,
                ConsumedAt = DateTime.UtcNow,
                RecordedBy = CurrentUserId
            };

            meal.IsConsumed = true;

            _db.MealConsumptions.Add(consumption);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Meal.Consume", "Meal",
                meal.ID, null, new { meal.StudentID, meal.MealDate, model.ScanMethod });

            return Json(new { success = true, message = "تم تسجيل الاستهلاك بنجاح" });
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentMeals(int? studentId, string? searchTerm)
        {
            Student? student = null;
            if (studentId.HasValue)
                student = await _db.Students.FirstOrDefaultAsync(s => s.ID == studentId.Value);
            else if (!string.IsNullOrEmpty(searchTerm))
                student = await _db.Students.FirstOrDefaultAsync(s => s.NationalID == searchTerm);

            if (student == null)
                return Json(new { success = false, message = "الطالب غير موجود" });

            var today = DateOnly.FromDateTime(DateTime.Today);
            var meals = await _db.Meals
                .Where(m => m.StudentID == student.ID && m.MealDate == today
                    && m.IsBooked == true && m.IsConsumed != true && m.IsActive == true)
                .Select(m => new
                {
                    m.ID,
                    m.MealType,
                    m.Price,
                    m.StudentID,
                    m.MealDate
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                student = new { student.ID, student.FullName, student.NationalID },
                meals
            });
        }

        [HttpGet]
        public async Task<IActionResult> Report(DateOnly? fromDate, DateOnly? toDate, int? cityId, string? mealType, int page = 1)
        {
            const int pageSize = 20;

            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name })
                .ToListAsync();

            var query = _db.Meals.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(m => m.MealDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(m => m.MealDate <= toDate.Value);
            if (cityId.HasValue)
                query = query.Where(m => m.DormitoryCityID == cityId.Value);
            if (!string.IsNullOrEmpty(mealType))
                query = query.Where(m => m.MealType == mealType);

            var totalGroups = await query
                .Select(m => new { m.MealDate, m.MealType })
                .Distinct()
                .CountAsync();

            var totalPages = (int)Math.Ceiling(totalGroups / (double)pageSize);
            if (totalPages < 1) totalPages = 1;

            var distinctGroups = await query
                .Select(m => new { m.MealDate, m.MealType })
                .Distinct()
                .OrderByDescending(g => g.MealDate)
                .ToListAsync();

            var pageGroups = distinctGroups
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var records = new List<MealReportRowViewModel>();
            foreach (var g in pageGroups)
            {
                var stats = await query
                    .Where(m => m.MealDate == g.MealDate && m.MealType == g.MealType)
                    .GroupBy(m => 1)
                    .Select(g2 => new
                    {
                        BookedCount = g2.Count(),
                        ConsumedCount = g2.Count(m => m.IsConsumed == true),
                        CancelledCount = g2.Count(m => m.IsActive == false),
                        TotalRevenue = g2.Where(m => m.IsConsumed == true).Sum(m => m.Price)
                    })
                    .FirstAsync();

                records.Add(new MealReportRowViewModel
                {
                    Date = g.MealDate,
                    MealType = g.MealType,
                    BookedCount = stats.BookedCount,
                    ConsumedCount = stats.ConsumedCount,
                    CancelledCount = stats.CancelledCount,
                    TotalRevenue = stats.TotalRevenue
                });
            }

            var totalMeals = await query.CountAsync();
            var totalConsumed = await query.CountAsync(m => m.IsConsumed == true);
            var totalCancelled = totalMeals - totalConsumed;

            return View(new MealReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                DormitoryCityID = cityId,
                MealType = mealType,
                Page = page,
                TotalPages = totalPages,
                TotalConsumed = totalConsumed,
                TotalCancelled = totalCancelled,
                TotalServed = totalMeals,
                Records = records,
                Cities = ViewBag.Cities ?? new List<CityLookup>()
            });
        }

        [HttpGet]
        public async Task<IActionResult> ReportExportExcel(DateOnly? fromDate, DateOnly? toDate, int? cityId, string? mealType)
        {
            var query = _db.Meals.AsQueryable();
            if (fromDate.HasValue) query = query.Where(m => m.MealDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(m => m.MealDate <= toDate.Value);
            if (cityId.HasValue) query = query.Where(m => m.DormitoryCityID == cityId.Value);
            if (!string.IsNullOrEmpty(mealType)) query = query.Where(m => m.MealType == mealType);
            var rows = await query.GroupBy(m => new { m.MealDate, m.MealType }).OrderByDescending(g => g.Key.MealDate).Select(g => new {
                Date = g.Key.MealDate, MealType = g.Key.MealType,
                BookedCount = g.Count(), ConsumedCount = g.Count(m => m.IsConsumed == true),
                CancelledCount = g.Count(m => m.IsActive == false), TotalRevenue = g.Where(m => m.IsConsumed == true).Sum(m => m.Price)
            }).ToListAsync();
            var columns = new[] { "التاريخ", "نوع الوجبة", "إجمالي الوجبات", "تم الاستهلاك", "ملغي", "إجمالي الإيرادات" };
            var data = _export.ExportToExcel("تقرير الوجبات", columns, rows, r => new object?[] { r.Date.ToString("yyyy-MM-dd"), r.MealType, r.BookedCount, r.ConsumedCount, r.CancelledCount, r.TotalRevenue });
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Meals.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ReportExportPdf(DateOnly? fromDate, DateOnly? toDate, int? cityId, string? mealType)
        {
            var query = _db.Meals.AsQueryable();
            if (fromDate.HasValue) query = query.Where(m => m.MealDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(m => m.MealDate <= toDate.Value);
            if (cityId.HasValue) query = query.Where(m => m.DormitoryCityID == cityId.Value);
            if (!string.IsNullOrEmpty(mealType)) query = query.Where(m => m.MealType == mealType);
            var rows = await query.GroupBy(m => new { m.MealDate, m.MealType }).OrderByDescending(g => g.Key.MealDate).Select(g => new {
                Date = g.Key.MealDate, MealType = g.Key.MealType,
                BookedCount = g.Count(), ConsumedCount = g.Count(m => m.IsConsumed == true),
                CancelledCount = g.Count(m => m.IsActive == false), TotalRevenue = g.Where(m => m.IsConsumed == true).Sum(m => m.Price)
            }).ToListAsync();
            var columns = new[] { "التاريخ", "نوع الوجبة", "إجمالي الوجبات", "تم الاستهلاك", "ملغي", "إجمالي الإيرادات" };
            var pdfRows = rows.Select(r => new[] { r.Date.ToString("yyyy-MM-dd"), r.MealType, r.BookedCount.ToString(), r.ConsumedCount.ToString(), r.CancelledCount.ToString(), r.TotalRevenue.ToString("N2") }).ToArray();
            var data = _export.ExportToPdf("تقرير الوجبات", columns, pdfRows);
            return File(data, "application/pdf", "Meals.pdf");
        }
    }
}
