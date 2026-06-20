using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Coordination;

namespace UniStay.Controllers
{
    [Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
    public class CoordinationController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly ICoordinationService _coordination;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;
        private readonly IReportExportService _export;

        public CoordinationController(
            AssuitDbContext db,
            ICoordinationService coordination,
            IAuditService audit,
            IEmailService email,
            IReportExportService export)
        {
            _db = db;
            _coordination = coordination;
            _audit = audit;
            _email = email;
            _export = export;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        private string GetCurrentAcademicYear()
        {
            var year = DateTime.Now.Year;
            return DateTime.Now.Month >= 9 ? $"{year}-{year + 1}" : $"{year - 1}-{year}";
        }

        [HttpGet]
        [RequirePermission("Coordination.Manage", "CanView")]
        public async Task<IActionResult> ConfigureRules(int? cityId)
        {
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive == true && c.IsDeleted != true)
                .ToListAsync();

            if (cityId == null)
            {
                var first = await _db.DormitoryCities
                    .Where(c => c.IsActive == true && c.IsDeleted != true)
                    .FirstOrDefaultAsync();
                if (first == null) return View(new CoordinationRulesViewModel());
                cityId = first.ID;
            }

            var rules = await _db.CoordinationRules
                .Where(r => r.DormitoryCityID == cityId)
                .OrderBy(r => r.Priority)
                .Select(r => new CoordinationRuleRowViewModel
                {
                    ID = r.ID,
                    RuleName = r.RuleName,
                    RuleType = r.RuleType,
                    Priority = r.Priority,
                    Weight = r.Weight,
                    IsActive = r.IsActive ?? false,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var city = await _db.DormitoryCities.FindAsync(cityId);

            return View(new CoordinationRulesViewModel
            {
                DormitoryCityID = cityId.Value,
                CityName = city?.Name ?? "",
                Rules = rules
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> ConfigureRules(CoordinationRulesViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("ConfigureRules", new { cityId = model.DormitoryCityID });

            var rule = new CoordinationRule
            {
                DormitoryCityID = model.DormitoryCityID,
                RuleName = model.NewRule.RuleName,
                RuleType = model.NewRule.RuleType,
                Priority = model.NewRule.Priority,
                Weight = model.NewRule.Weight,
                IsActive = model.NewRule.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };

            _db.CoordinationRules.Add(rule);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "CoordinationRule.Create", "CoordinationRule",
                rule.ID, null, new { rule.RuleName, rule.RuleType, rule.Priority, rule.Weight });

            TempData["Success"] = "تم إضافة القاعدة بنجاح";
            return RedirectToAction("ConfigureRules", new { cityId = model.DormitoryCityID });
        }

        [HttpGet]
        [RequirePermission("Coordination.View", "CanView")]
        public async Task<IActionResult> Preview(int? cityId)
        {
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive == true && c.IsDeleted != true)
                .ToListAsync();

            if (cityId == null)
            {
                var first = await _db.DormitoryCities
                    .Where(c => c.IsActive == true && c.IsDeleted != true)
                    .FirstOrDefaultAsync();
                if (first == null) return View(new CoordinationPreviewViewModel());
                cityId = first.ID;
            }

            var city = await _db.DormitoryCities.FindAsync(cityId);
            var year = GetCurrentAcademicYear();

            try
            {
                var preview = await _coordination.PreviewAsync(cityId.Value, year);

                var availableBeds = await _db.CityRooms
                    .Where(r => r.CityBuilding != null && r.CityBuilding.DormitoryCityID == cityId
                        && r.IsActive == true && r.IsDeleted != true)
                    .SumAsync(r => (int)r.BedsCount - (int)r.CurrentOccupancy);

                return View(new CoordinationPreviewViewModel
                {
                    DormitoryCityID = cityId.Value,
                    CityName = city?.Name ?? "",
                    AcademicYear = year,
                    TotalApplicants = preview.TotalApplicants,
                    AvailableBeds = availableBeds,
                    AcceptedCount = preview.AcceptedCount,
                    WaitlistCount = preview.WaitlistCount,
                    TopStudents = preview.TopStudents?.Select(s => new CoordinationPreviewStudentViewModel
                    {
                        Name = s.Name ?? "",
                        Score = s.Score ?? 0
                    }).ToList() ?? new()
                });
            }
            catch
            {
                TempData["Error"] = "حدث خطأ أثناء معاينة التنسيق";
                return View(new CoordinationPreviewViewModel
                {
                    DormitoryCityID = cityId.Value,
                    CityName = city?.Name ?? "",
                    AcademicYear = year
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> Run(int cityId, string? academicYear)
        {
            var year = academicYear ?? GetCurrentAcademicYear();

            try
            {
                var result = await _coordination.RunAsync(cityId, year, CurrentUserId);

                await _audit.LogAsync(CurrentUserId, "Staff", "Coordination.Run", "Application",
                    null, null, new { cityId, academicYear = year, result.Accepted, result.Waitlist });

                TempData["Success"] = $"تم تنفيذ التنسيق: {result.Accepted} مقبول، {result.Waitlist} انتظار";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطأ في تنفيذ التنسيق: {ex.Message}";
            }

            return RedirectToAction("Results", new { cityId });
        }

        [HttpGet]
        [RequirePermission("Coordination.View", "CanView")]
        public async Task<IActionResult> Results(int? cityId, int page = 1)
        {
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive == true && c.IsDeleted != true)
                .ToListAsync();

            if (cityId == null)
            {
                var first = await _db.DormitoryCities
                    .Where(c => c.IsActive == true && c.IsDeleted != true)
                    .FirstOrDefaultAsync();
                if (first == null) return View(new CoordinationResultsViewModel());
                cityId = first.ID;
            }

            var year = GetCurrentAcademicYear();
            var city = await _db.DormitoryCities.FindAsync(cityId);

            var query = _db.CoordinationResults
                .Include(r => r.Student)
                .Where(r => r.DormitoryCityID == cityId && r.AcademicYear == year);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / 20.0);

            var results = await query
                .OrderBy(r => r.Rank)
                .Skip((page - 1) * 20)
                .Take(20)
                .Select(r => new CoordinationResultRowViewModel
                {
                    ID = r.ID,
                    StudentName = r.Student!.FullName,
                    NationalID = r.Student.NationalID,
                    Faculty = r.Student.Faculty,
                    TotalScore = r.TotalScore,
                    Rank = r.Rank,
                    Status = r.Status ?? "",
                    ProcessedAt = r.ProcessedAt
                })
                .ToListAsync();

            var accepted = await query.CountAsync(r => r.Status == "Accepted");
            var waitlist = await query.CountAsync(r => r.Status == "Waitlist");
            var rejected = await query.CountAsync(r => r.Status == "Rejected" || r.Status == "Pending");

            return View(new CoordinationResultsViewModel
            {
                DormitoryCityID = cityId.Value,
                CityName = city?.Name ?? "",
                AcademicYear = year,
                Total = total,
                AcceptedCount = accepted,
                WaitlistCount = waitlist,
                RejectedCount = rejected,
                Results = results,
                Page = page,
                TotalPages = totalPages
            });
        }

        [HttpGet]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> ManualOverride(int id)
        {
            var result = await _db.CoordinationResults
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.ID == id);

            if (result == null) return Json(new { success = false, message = "النتيجة غير موجودة" });

            return Json(new
            {
                success = true,
                data = new ManualOverrideViewModel
                {
                    ID = result.ID,
                    StudentName = result.Student?.FullName ?? "",
                    NationalID = result.Student?.NationalID,
                    Faculty = result.Student?.Faculty,
                    DistanceScore = result.DistanceScore,
                    GradeScore = result.GradeScore,
                    AgeScore = result.AgeScore,
                    SpecialBonus = result.SpecialBonus,
                    TotalScore = result.TotalScore,
                    Rank = result.Rank
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> ManualOverrideSave([FromBody] ManualOverrideSaveViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صالحة" });

            var result = await _db.CoordinationResults.FindAsync(model.ID);
            if (result == null)
                return Json(new { success = false, message = "النتيجة غير موجودة" });

            var oldScore = result.TotalScore;

            result.DistanceScore = model.DistanceScore;
            result.GradeScore = model.GradeScore;
            result.AgeScore = model.AgeScore;
            result.SpecialBonus = model.SpecialBonus;
            result.TotalScore = (model.DistanceScore ?? 0) + (model.GradeScore ?? 0) + (model.AgeScore ?? 0) + (model.SpecialBonus ?? 0);

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Coordination.ManualOverride", "CoordinationResult",
                result.ID, new { TotalScore = oldScore }, new { TotalScore = result.TotalScore, model.DistanceScore, model.GradeScore, model.AgeScore, model.SpecialBonus });

            return Json(new { success = true, totalScore = result.TotalScore, message = "تم تعديل الدرجات بنجاح" });
        }

        [HttpGet]
        [RequirePermission("Coordination.View", "CanView")]
        public async Task<IActionResult> SpecialCases(int? cityId, int page = 1)
        {
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive == true && c.IsDeleted != true)
                .ToListAsync();

            if (cityId == null)
            {
                var first = await _db.DormitoryCities
                    .Where(c => c.IsActive == true && c.IsDeleted != true)
                    .FirstOrDefaultAsync();
                if (first == null) return View(new SpecialCasesViewModel());
                cityId = first.ID;
            }

            var city = await _db.DormitoryCities.FindAsync(cityId);

            var query = _db.SpecialCases
                .Include(s => s.Student)
                .Include(s => s.Application)
                .Where(s => s.Application!.DormitoryCityID == cityId);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / 20.0);

            var cases = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * 20)
                .Take(20)
                .Select(s => new SpecialCaseRowViewModel
                {
                    ID = s.ID,
                    StudentName = s.Student!.FullName,
                    NationalID = s.Student.NationalID,
                    CaseType = s.CaseType,
                    Description = s.Description,
                    Status = s.Status ?? "",
                    ReviewNotes = s.ReviewNotes,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return View(new SpecialCasesViewModel
            {
                DormitoryCityID = cityId.Value,
                CityName = city?.Name ?? "",
                SpecialCases = cases,
                Page = page,
                TotalPages = totalPages
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> AddSpecialCase(AddSpecialCaseViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "بيانات غير صالحة" });

            var special = new SpecialCase
            {
                StudentID = model.StudentID,
                ApplicationID = model.ApplicationID,
                CaseType = model.CaseType,
                Description = model.Description,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.SpecialCases.Add(special);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "SpecialCase.Create", "SpecialCase",
                special.ID, null, new { special.StudentID, special.CaseType });

            return Json(new { success = true, message = "تم إضافة الحالة الخاصة" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> ReviewSpecialCase(ReviewSpecialCaseViewModel model)
        {
            var sc = await _db.SpecialCases.FindAsync(model.ID);
            if (sc == null) return Json(new { success = false, message = "الحالة غير موجودة" });

            var oldStatus = sc.Status;
            sc.Status = model.Status;
            sc.ReviewNotes = model.ReviewNotes;
            sc.ReviewedBy = CurrentUserId;
            sc.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "SpecialCase.Review", "SpecialCase",
                model.ID, new { Status = oldStatus }, new { Status = model.Status, model.ReviewNotes });

            return Json(new { success = true, message = "تم مراجعة الحالة" });
        }

        [HttpGet]
        [RequirePermission("Coordination.View", "CanView")]
        public async Task<IActionResult> Waitlist(int? cityId, int page = 1)
        {
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive == true && c.IsDeleted != true)
                .ToListAsync();

            if (cityId == null)
            {
                var first = await _db.DormitoryCities
                    .Where(c => c.IsActive == true && c.IsDeleted != true)
                    .FirstOrDefaultAsync();
                if (first == null) return View(new WaitlistViewModel());
                cityId = first.ID;
            }

            var year = GetCurrentAcademicYear();
            var city = await _db.DormitoryCities.FindAsync(cityId);

            var query = _db.CoordinationResults
                .Include(r => r.Student)
                .Where(r => r.DormitoryCityID == cityId && r.AcademicYear == year && r.Status == "Waitlist");

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / 20.0);

            var waitlisted = await query
                .OrderBy(r => r.Rank)
                .Skip((page - 1) * 20)
                .Take(20)
                .Select(r => new WaitlistRowViewModel
                {
                    ID = r.ID,
                    StudentName = r.Student!.FullName,
                    NationalID = r.Student.NationalID,
                    TotalScore = r.TotalScore,
                    Rank = r.Rank
                })
                .ToListAsync();

            return View(new WaitlistViewModel
            {
                DormitoryCityID = cityId.Value,
                CityName = city?.Name ?? "",
                AcademicYear = year,
                TotalWaitlisted = total,
                Waitlisted = waitlisted,
                Page = page,
                TotalPages = totalPages
            });
        }

        [HttpGet]
        [RequirePermission("Coordination.View", "CanView")]
        public async Task<IActionResult> FacultyQuotas(int? cityId)
        {
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive == true && c.IsDeleted != true)
                .ToListAsync();

            if (cityId == null)
            {
                var first = await _db.DormitoryCities
                    .Where(c => c.IsActive == true && c.IsDeleted != true)
                    .FirstOrDefaultAsync();
                if (first == null) return View(new FacultyQuotasViewModel());
                cityId = first.ID;
            }

            var year = GetCurrentAcademicYear();
            var city = await _db.DormitoryCities.FindAsync(cityId);

            var quotas = await _db.FacultyQuota
                .Where(q => q.DormitoryCityID == cityId && q.AcademicYear == year)
                .Select(q => new FacultyQuotaRowViewModel
                {
                    ID = q.ID,
                    Faculty = q.Faculty,
                    MaxQuota = q.MaxQuota,
                    MinQuota = q.MinQuota,
                    CurrentCount = q.CurrentCount
                })
                .ToListAsync();

            return View(new FacultyQuotasViewModel
            {
                DormitoryCityID = cityId.Value,
                CityName = city?.Name ?? "",
                AcademicYear = year,
                Quotas = quotas
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> AddFacultyQuota(AddFacultyQuotaViewModel model, int cityId)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "بيانات غير صالحة" });

            var year = GetCurrentAcademicYear();

            var exists = await _db.FacultyQuota.AnyAsync(q =>
                q.DormitoryCityID == cityId && q.AcademicYear == year && q.Faculty == model.Faculty);

            if (exists) return Json(new { success = false, message = "هذه الكلية موجودة مسبقاً" });

            var quota = new FacultyQuotum
            {
                DormitoryCityID = cityId,
                AcademicYear = year,
                Faculty = model.Faculty,
                MaxQuota = model.MaxQuota,
                MinQuota = model.MinQuota,
                CurrentCount = 0
            };

            _db.FacultyQuota.Add(quota);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "FacultyQuota.Create", "FacultyQuota",
                quota.ID, null, new { quota.Faculty, quota.MaxQuota });

            return Json(new { success = true, message = "تم إضافة الحصة" });
        }

        [HttpGet]
        [RequirePermission("Coordination.Manage", "CanView")]
        public async Task<IActionResult> ExportResultsExcel(int? cityId)
        {
            var query = _db.CoordinationResults
                .Include(r => r.Student).Include(r => r.DormitoryCity)
                .AsQueryable();

            if (cityId.HasValue)
                query = query.Where(r => r.DormitoryCityID == cityId.Value);

            var results = await query.OrderBy(r => r.Rank).ToListAsync();

            var columns = new[] { "الترتيب", "الاسم", "الرقم القومي", "الكلية", "المدينة", "درجة المسافة", "الدرجة الأكاديمية", "مكافأة خاصة", "المجموع", "الحالة" };
            var data = _export.ExportToExcel("نتائج التنسيق", columns, results, r => new object?[] {
                r.Rank, r.Student?.FullName, r.Student?.NationalID, r.Student?.Faculty,
                r.DormitoryCity?.Name, r.DistanceScore, r.GradeScore, r.SpecialBonus,
                r.TotalScore, r.Status
            });
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CoordinationResults.xlsx");
        }

        [HttpGet]
        [RequirePermission("Coordination.Manage", "CanView")]
        public async Task<IActionResult> ExportResultsPdf(int? cityId)
        {
            var query = _db.CoordinationResults
                .Include(r => r.Student).Include(r => r.DormitoryCity)
                .AsQueryable();

            if (cityId.HasValue)
                query = query.Where(r => r.DormitoryCityID == cityId.Value);

            var results = await query.OrderBy(r => r.Rank).ToListAsync();

            var columns = new[] { "الترتيب", "الاسم", "الكلية", "المجموع", "الحالة" };
            var rows = results.Select(r => new[] {
                r.Rank?.ToString() ?? "", r.Student?.FullName ?? "",
                r.Student?.Faculty ?? "", r.TotalScore?.ToString("F2") ?? "",
                r.Status ?? ""
            }).ToArray();

            var pdf = _export.ExportToPdf("نتائج التنسيق", columns, rows);
            return File(pdf, "application/pdf", "CoordinationResults.pdf");
        }

        [HttpGet]
        [RequirePermission("Coordination.Manage", "CanView")]
        public async Task<IActionResult> ReportCoordinationResults(int? cityId)
        {
            var query = _db.CoordinationResults
                .Include(r => r.Student).Include(r => r.DormitoryCity)
                .AsQueryable();

            if (cityId.HasValue)
                query = query.Where(r => r.DormitoryCityID == cityId.Value);

            var results = await query.OrderBy(r => r.Rank).ToListAsync();

            ViewBag.Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted).ToListAsync();
            ViewBag.TotalAccepted = results.Count(r => r.Status == "Accepted");
            ViewBag.TotalWaitlist = results.Count(r => r.Status == "Waitlist");

            return View("~/Views/Coordination/CoordinationReport.cshtml", results);
        }
    }
}
