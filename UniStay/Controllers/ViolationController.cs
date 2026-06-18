using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Violation;

namespace UniStay.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class ViolationController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;
        private readonly IReportExportService _export;

        public ViolationController(AssuitDbContext db, IAuditService audit, IEmailService email, IReportExportService export)
        {
            _db = db;
            _audit = audit;
            _email = email;
            _export = export;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Add");
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewBag.ViolationTypes = new List<SelectListItem>
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

            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new SelectListItem { Value = c.ID.ToString(), Text = c.Name })
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddViolationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ViolationTypes = new List<SelectListItem>
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

                ViewBag.Cities = await _db.DormitoryCities
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .Select(c => new SelectListItem { Value = c.ID.ToString(), Text = c.Name })
                    .ToListAsync();

                return View(model);
            }

            var student = await _db.Students.FindAsync(model.StudentID);
            if (student == null)
            {
                TempData["Error"] = "الطالب غير موجود";
                return RedirectToAction("Add");
            }

            var violation = new Violation
            {
                StudentID = model.StudentID,
                DormitoryCityID = model.DormitoryCityID,
                ViolationType = model.ViolationType,
                Severity = model.Severity,
                FineAmount = model.FineAmount,
                Description = model.Description,
                Status = "Active",
                RecordedBy = CurrentUserId,
                RecordedAt = DateTime.UtcNow,
                IsOnBlacklist = false
            };

            _db.Violations.Add(violation);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Violation.Add",
                "Violation", violation.ID, null,
                new { model.StudentID, model.ViolationType, model.Severity, model.FineAmount });

            TempData["Success"] = "تم تسجيل المخالفة بنجاح";
            return RedirectToAction("Add");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayFine(int id, decimal? amount = null)
        {
            var violation = await _db.Violations.FindAsync(id);
            if (violation == null)
                return Json(new { success = false, message = "المخالفة غير موجودة" });

            violation.FinePaid = amount ?? violation.FineAmount;
            violation.Status = "Paid";
            violation.ResolvedBy = CurrentUserId;
            violation.ResolvedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Violation.PayFine",
                "Violation", id,
                null, new { Amount = violation.FinePaid, violation.Status });

            return Json(new { success = true, message = "تم دفع الغرامة بنجاح" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Blacklist(int id)
        {
            var violation = await _db.Violations.FindAsync(id);
            if (violation == null)
                return Json(new { success = false, message = "المخالفة غير موجودة" });

            violation.IsOnBlacklist = true;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Violation.Blacklist",
                "Violation", id, null, new { IsOnBlacklist = true });

            return Json(new { success = true, message = "تم إضافة الطالب إلى القائمة السوداء" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var violation = await _db.Violations.FindAsync(id);
            if (violation == null)
                return Json(new { success = false, message = "المخالفة غير موجودة" });

            violation.Status = "Cancelled";
            violation.ResolvedBy = CurrentUserId;
            violation.ResolvedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Violation.Cancel",
                "Violation", id,
                null, new { Status = "Cancelled" });

            return Json(new { success = true, message = "تم إلغاء المخالفة" });
        }

        [HttpGet]
        public async Task<IActionResult> Report(
            string? filterStatus = null,
            string? filterSeverity = null,
            int? dormitoryCityId = null,
            int page = 1)
        {
            var query = _db.Violations
                .Include(v => v.Student)
                .Include(v => v.RecordedByNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "All")
                query = query.Where(v => v.Status == filterStatus);

            if (!string.IsNullOrEmpty(filterSeverity) && filterSeverity != "All")
                query = query.Where(v => v.Severity == filterSeverity);

            if (dormitoryCityId.HasValue)
                query = query.Where(v => v.DormitoryCityID == dormitoryCityId.Value);

            var total = await query.CountAsync();

            var violations = await query
                .OrderByDescending(v => v.RecordedAt)
                .Skip((page - 1) * 30)
                .Take(30)
                .Select(v => new ViolationRowViewModel
                {
                    ID = v.ID,
                    StudentName = v.Student.FullName,
                    NationalID = v.Student.NationalID,
                    ViolationType = v.ViolationType,
                    Severity = v.Severity,
                    FineAmount = v.FineAmount,
                    FinePaid = v.FinePaid,
                    Status = v.Status,
                    IsOnBlacklist = v.IsOnBlacklist == true,
                    RecordedAt = v.RecordedAt,
                    RecordedByName = v.RecordedByNavigation!.Name
                })
                .ToListAsync();

            var vm = new ViolationReportViewModel
            {
                Violations = violations,
                FilterStatus = filterStatus,
                FilterSeverity = filterSeverity,
                DormitoryCityID = dormitoryCityId,
                Page = page,
                TotalPages = (int)Math.Ceiling(total / 30.0),
                Cities = await _db.DormitoryCities
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .Select(c => new CityLookup { ID = c.ID, Name = c.Name })
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ReportExportExcel(string? filterStatus = null, string? filterSeverity = null, int? dormitoryCityId = null)
        {
            var query = _db.Violations.Include(v => v.Student).AsQueryable();
            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "All") query = query.Where(v => v.Status == filterStatus);
            if (!string.IsNullOrEmpty(filterSeverity) && filterSeverity != "All") query = query.Where(v => v.Severity == filterSeverity);
            if (dormitoryCityId.HasValue) query = query.Where(v => v.DormitoryCityID == dormitoryCityId.Value);
            var rows = await query.OrderByDescending(v => v.RecordedAt).Select(v => new {
                v.Student.FullName, v.Student.NationalID, v.ViolationType, v.Severity, v.FineAmount, v.FinePaid, v.Status, v.RecordedAt
            }).ToListAsync();
            var columns = new[] { "الطالب", "الرقم القومي", "نوع المخالفة", "درجة الخطورة", "الغرامة", "المدفوع", "الحالة", "التاريخ" };
            var data = _export.ExportToExcel("المخالفات", columns, rows, r => new object?[] { r.FullName, r.NationalID, r.ViolationType, r.Severity, r.FineAmount, r.FinePaid, r.Status, r.RecordedAt?.ToString("yyyy-MM-dd") });
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Violations.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ReportExportPdf(string? filterStatus = null, string? filterSeverity = null, int? dormitoryCityId = null)
        {
            var query = _db.Violations.Include(v => v.Student).AsQueryable();
            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "All") query = query.Where(v => v.Status == filterStatus);
            if (!string.IsNullOrEmpty(filterSeverity) && filterSeverity != "All") query = query.Where(v => v.Severity == filterSeverity);
            if (dormitoryCityId.HasValue) query = query.Where(v => v.DormitoryCityID == dormitoryCityId.Value);
            var rows = await query.OrderByDescending(v => v.RecordedAt).Select(v => new {
                v.Student.FullName, v.Student.NationalID, v.ViolationType, v.Severity, v.FineAmount, v.FinePaid, v.Status, v.RecordedAt
            }).ToListAsync();
            var columns = new[] { "الطالب", "الرقم القومي", "نوع المخالفة", "درجة الخطورة", "الغرامة", "المدفوع", "الحالة", "التاريخ" };
            var pdfRows = rows.Select(r => new[] { r.FullName, r.NationalID, r.ViolationType, r.Severity, r.FineAmount?.ToString("N2") ?? "", r.FinePaid?.ToString("N2") ?? "", r.Status, r.RecordedAt?.ToString("yyyy-MM-dd") ?? "" }).ToArray();
            var data = _export.ExportToPdf("المخالفات", columns, pdfRows);
            return File(data, "application/pdf", "Violations.pdf");
        }
    }
}
