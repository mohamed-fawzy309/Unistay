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
            return DateTime.Now.Month >= 6 ? $"{year}-{year + 1}" : $"{year - 1}-{year}";
        }

        private async Task<List<DormitoryCity>> LoadActiveCitiesAsync() =>
            await _db.DormitoryCities
                .Where(c => c.IsActive == true && c.IsDeleted != true)
                .ToListAsync();

        // ============================================================
        // ConfigureRules (GET)
        // ============================================================

        [HttpGet]
        [RequirePermission("Coordination.Manage", "CanView")]
        public async Task<IActionResult> ConfigureRules(int? cityId)
        {
            var cities = await LoadActiveCitiesAsync();
            ViewBag.Cities = cities;

            if (cityId == null)
            {
                var firstCity = cities.FirstOrDefault();
                if (firstCity == null) return View(new CoordinationRulesViewModel());
                cityId = firstCity.ID;
            }

            return await BuildConfigureRulesView(cityId.Value);
        }

        private async Task<IActionResult> BuildConfigureRulesView(int cityId)
        {
            ViewBag.Cities = await LoadActiveCitiesAsync();
            ViewBag.Faculties = await _db.Faculties.Where(f => f.IsActive == true).OrderBy(f => f.Name).ToListAsync();

            var city = await _db.DormitoryCities.FindAsync(cityId);
            if (city == null)
            {
                TempData["Error"] = "المدينة غير موجودة";
                return View(new CoordinationRulesViewModel());
            }

            var rules = await _db.CoordinationRules
                .Where(r => r.DormitoryCityID == cityId)
                .OrderBy(r => r.Priority).ThenBy(r => r.ID)
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

            return View(new CoordinationRulesViewModel
            {
                DormitoryCityID = cityId,
                CityName = city.Name,
                Rules = rules
            });
        }

        // ============================================================
        // ConfigureRules (POST) — Add Rule
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> ConfigureRules(int DormitoryCityID, CreateCoordinationRuleViewModel NewRule)
        {
            ViewBag.Cities = await LoadActiveCitiesAsync();

            if (!ModelState.IsValid)
            {
                return await BuildConfigureRulesView(DormitoryCityID);
            }

            var city = await _db.DormitoryCities.FindAsync(DormitoryCityID);
            if (city == null)
            {
                ModelState.AddModelError("", "المدينة المحددة غير موجودة");
                return await BuildConfigureRulesView(DormitoryCityID);
            }

            var existingRules = await _db.CoordinationRules
                .Where(r => r.DormitoryCityID == DormitoryCityID)
                .ToListAsync();

            if (existingRules.Any(r => r.RuleType == NewRule.RuleType && r.RuleType != CoordinationRuleTypes.Faculty))
            {
                ModelState.AddModelError("NewRule.RuleType", "يوجد قاعدة بنفس النوع بالفعل لهذه المدينة");
                return await BuildConfigureRulesView(DormitoryCityID);
            }

            if (NewRule.RuleType == CoordinationRuleTypes.Faculty && existingRules.Any(r => r.RuleType == CoordinationRuleTypes.Faculty && r.RuleName == NewRule.RuleName))
            {
                ModelState.AddModelError("NewRule.RuleName", "يوجد قاعدة لهذه الكلية بالفعل");
                return await BuildConfigureRulesView(DormitoryCityID);
            }

            if (existingRules.Any(r => r.Priority == NewRule.Priority))
            {
                ModelState.AddModelError("NewRule.Priority", "يوجد قاعدة بنفس الأولوية بالفعل لهذه المدينة");
                return await BuildConfigureRulesView(DormitoryCityID);
            }

            var rule = new CoordinationRule
            {
                DormitoryCityID = DormitoryCityID,
                RuleName = NewRule.RuleName,
                RuleType = NewRule.RuleType,
                Priority = NewRule.Priority,
                Weight = NewRule.Weight,
                IsActive = NewRule.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };

            _db.CoordinationRules.Add(rule);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "CoordinationRule.Create", "CoordinationRule",
                rule.ID, null, new { rule.RuleName, rule.RuleType, rule.Priority, rule.Weight });

            TempData["Success"] = "تم إضافة القاعدة بنجاح";
            return RedirectToAction("ConfigureRules", new { cityId = DormitoryCityID });
        }

        // ============================================================
        // EditRule (GET) — returns JSON for modal
        // ============================================================

        [HttpGet]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> EditRule(int id)
        {
            var rule = await _db.CoordinationRules.FindAsync(id);
            if (rule == null)
                return Json(new { success = false, message = "القاعدة غير موجودة" });

            return Json(new
            {
                success = true,
                data = new EditCoordinationRuleViewModel
                {
                    ID = rule.ID,
                    RuleName = rule.RuleName,
                    RuleType = rule.RuleType,
                    Priority = rule.Priority,
                    Weight = rule.Weight,
                    IsActive = rule.IsActive ?? false
                }
            });
        }

        // ============================================================
        // EditRule (POST) — Save edited rule
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> EditRule(EditCoordinationRuleViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صالحة", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var rule = await _db.CoordinationRules.FindAsync(model.ID);
            if (rule == null)
                return Json(new { success = false, message = "القاعدة غير موجودة" });

            var cityId = rule.DormitoryCityID;

            var city = await _db.DormitoryCities.FindAsync(cityId);
            if (city == null)
                return Json(new { success = false, message = "المدينة غير موجودة" });

            var existingRules = await _db.CoordinationRules
                .Where(r => r.DormitoryCityID == cityId && r.ID != model.ID)
                .ToListAsync();

            if (existingRules.Any(r => r.RuleType == model.RuleType && r.RuleType != CoordinationRuleTypes.Faculty))
                return Json(new { success = false, message = "يوجد قاعدة بنفس النوع بالفعل لهذه المدينة" });

            if (model.RuleType == CoordinationRuleTypes.Faculty && existingRules.Any(r => r.RuleType == CoordinationRuleTypes.Faculty && r.RuleName == model.RuleName))
                return Json(new { success = false, message = "يوجد قاعدة لهذه الكلية بالفعل" });

            if (existingRules.Any(r => r.Priority == model.Priority))
                return Json(new { success = false, message = "يوجد قاعدة بنفس الأولوية بالفعل لهذه المدينة" });

            var oldValues = new { rule.RuleName, rule.RuleType, rule.Priority, rule.Weight, IsActive = rule.IsActive ?? false };

            rule.RuleName = model.RuleName;
            rule.RuleType = model.RuleType;
            rule.Priority = model.Priority;
            rule.Weight = model.Weight;
            rule.IsActive = model.IsActive;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "CoordinationRule.Update", "CoordinationRule",
                rule.ID, oldValues, new { model.RuleName, model.RuleType, model.Priority, model.Weight, model.IsActive });

            return Json(new { success = true, message = "تم تعديل القاعدة بنجاح" });
        }

        // ============================================================
        // DeleteRule (POST)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> DeleteRule(int id)
        {
            var rule = await _db.CoordinationRules.FindAsync(id);
            if (rule == null)
                return Json(new { success = false, message = "القاعدة غير موجودة" });

            var oldValues = new { rule.RuleName, rule.RuleType, rule.Priority, rule.Weight };

            _db.CoordinationRules.Remove(rule);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "CoordinationRule.Delete", "CoordinationRule",
                id, oldValues, null);

            return Json(new { success = true, message = "تم حذف القاعدة بنجاح" });
        }

        // ============================================================
        // ToggleRuleActive (POST)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> ToggleRuleActive(int id)
        {
            var rule = await _db.CoordinationRules.FindAsync(id);
            if (rule == null)
                return Json(new { success = false, message = "القاعدة غير موجودة" });

            var oldActive = rule.IsActive ?? false;
            rule.IsActive = !oldActive;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "CoordinationRule.ToggleActive", "CoordinationRule",
                rule.ID, new { IsActive = oldActive }, new { IsActive = rule.IsActive });

            return Json(new { success = true, isActive = rule.IsActive, message = rule.IsActive == true ? "تم تفعيل القاعدة" : "تم إلغاء تفعيل القاعدة" });
        }

        // ============================================================
        // Preview
        // ============================================================

        [HttpGet]
        [RequirePermission("Coordination.View", "CanView")]
        public async Task<IActionResult> Preview(int? cityId)
        {
            ViewBag.Cities = await LoadActiveCitiesAsync();

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
                        && r.IsActive == true && r.IsDeleted != true
                        && r.RoomType != "إشراف" && r.RoomType != "مخزن")
                    .SumAsync(r => Convert.ToInt32(r.BedsCount) - Convert.ToInt32(r.CurrentOccupancy));

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
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(new CoordinationPreviewViewModel
                {
                    DormitoryCityID = cityId.Value,
                    CityName = city?.Name ?? "",
                    AcademicYear = year
                });
            }
        }

        // ============================================================
        // Run (POST)
        // ============================================================

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

        // ============================================================
        // Results (GET)
        // ============================================================

        [HttpGet]
        [RequirePermission("Coordination.View", "CanView")]
        public async Task<IActionResult> Results(int? cityId, int page = 1)
        {
            ViewBag.Cities = await LoadActiveCitiesAsync();

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

        // ============================================================
        // ManualOverride (GET) — returns JSON
        // ============================================================

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

        // ============================================================
        // ManualOverrideSave (POST)
        // ============================================================

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

        // ============================================================
        // Waitlist (GET)
        // ============================================================

        [HttpGet]
        [RequirePermission("Coordination.View", "CanView")]
        public async Task<IActionResult> Waitlist(int? cityId, int page = 1)
        {
            ViewBag.Cities = await LoadActiveCitiesAsync();

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

        // ============================================================
        // ExportResultsExcel (GET)
        // ============================================================

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

        // ============================================================
        // ExportResultsPdf (GET)
        // ============================================================

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


    }
}
