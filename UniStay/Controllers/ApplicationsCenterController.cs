using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels;
using UniStay.ViewModels.Applications;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
public class ApplicationsCenterController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;
    private readonly IReportExportService _export;
    private readonly IUniversityApiService _api;

    public ApplicationsCenterController(
        AssuitDbContext db, IAuditService audit, IEmailService email,
        IReportExportService export, IUniversityApiService api)
    {
        _db = db;
        _audit = audit;
        _email = email;
        _export = export;
        _api = api;
    }

    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    // =====================================================================
    // INDEX
    // =====================================================================
    [HttpGet]
    [RequirePermission("Applications.View", "CanView")]
    public async Task<IActionResult> Index(
        string? search = null, string? status = null, string? studentType = null,
        int? cityId = null, string? faculty = null,
        DateTime? fromDate = null, DateTime? toDate = null,
        string? sortBy = null, string? sortDir = null, int page = 1)
    {
        const int pageSize = 30;

        var query = _db.Applications
            .Include(a => a.Student)
            .Include(a => a.DormitoryCity)
            .Include(a => a.ReviewedByNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(a => a.Student!.FullName.Contains(search) || a.Student.NationalID.Contains(search));
        if (!string.IsNullOrEmpty(status) && status != "All")
            query = query.Where(a => a.Status == status);
        if (!string.IsNullOrEmpty(studentType))
            query = query.Where(a => a.StudentType == studentType);
        if (cityId.HasValue)
            query = query.Where(a => a.DormitoryCityID == cityId.Value);
        if (!string.IsNullOrEmpty(faculty))
            query = query.Where(a => a.Student!.Faculty == faculty);
        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value.AddDays(1));

        sortBy ??= "CreatedAt";
        sortDir ??= "desc";
        query = (sortBy, sortDir) switch
        {
            ("StudentName", "asc") => query.OrderBy(a => a.Student!.FullName),
            ("StudentName", "desc") => query.OrderByDescending(a => a.Student!.FullName),
            ("Status", "asc") => query.OrderBy(a => a.Status),
            ("Status", "desc") => query.OrderByDescending(a => a.Status),
            _ => query.OrderByDescending(a => a.CreatedAt)
        };

        var total = await query.CountAsync();

        var apps = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ApplicationRowViewModel
            {
                ID = a.ID,
                StudentName = a.Student!.FullName,
                NationalID = a.Student.NationalID,
                Faculty = a.Student.Faculty,
                CityName = a.DormitoryCity.Name,
                StudentType = a.StudentType,
                HousingType = a.HousingType,
                Status = a.Status,
                StatusDisplay = MapStatus(a.Status),
                CreatedAt = a.CreatedAt!.Value,
                ServerVerificationStatus = a.ServerVerificationStatus,
                CoordinationScore = a.CoordinationScore,
                CoordinationRank = a.CoordinationRank,
                ReviewedByName = a.ReviewedByNavigation!.Name
            })
            .ToListAsync();

        var vm = new ApplicationsIndexViewModel
        {
            Applications = apps,
            Filter = new ApplicationsFilterViewModel
            {
                Search = search,
                Status = status,
                StudentType = studentType,
                CityID = cityId,
                Faculty = faculty,
                FromDate = fromDate,
                ToDate = toDate,
                SortBy = sortBy,
                SortDir = sortDir
            },
            TotalCount = total,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            PendingCount = await _db.Applications.CountAsync(a => a.Status == "Pending"),
            AcceptedCount = await _db.Applications.CountAsync(a => a.Status == "Accepted"),
            RejectedCount = await _db.Applications.CountAsync(a => a.Status == "Rejected"),
            UnderReviewCount = await _db.Applications.CountAsync(a => a.Status == "UnderReview"),
            Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync()
        };

        return View(vm);
    }

    // =====================================================================
    // DETAILS  ← FIX: أضفنا Route صريح بـ id اختياري مع Guard
    // =====================================================================
    [HttpGet("{controller}/Details/{id:int}")]
    [RequirePermission("Applications.View", "CanView")]
    public async Task<IActionResult> Details(int id)
    {
        var app = await _db.Applications
            .Include(a => a.Student).ThenInclude(s => s!.Guardians)
            .Include(a => a.DormitoryCity)
            .Include(a => a.ReviewedByNavigation)
            .Include(a => a.Allocation).ThenInclude(al => al!.CityRoom).ThenInclude(r => r.CityBuilding)
            .FirstOrDefaultAsync(a => a.ID == id);

        if (app == null) return NotFound();

        var vm = BuildDetailViewModel(app);
        return View(vm);
    }

    // =====================================================================
    // PRINT  ← FIX: أضفنا Route صريح بـ id اختياري مع Guard
    // =====================================================================
    [HttpGet("{controller}/Print/{id:int}")]
    [RequirePermission("Applications.View", "CanView")]
    public async Task<IActionResult> Print(int id)
    {
        var app = await _db.Applications
            .Include(a => a.Student).ThenInclude(s => s!.Guardians)
            .Include(a => a.DormitoryCity)
            .Include(a => a.ReviewedByNavigation)
            .Include(a => a.Allocation).ThenInclude(al => al!.CityRoom).ThenInclude(r => r.CityBuilding)
            .FirstOrDefaultAsync(a => a.ID == id);

        if (app == null) return NotFound();

        var vm = BuildDetailViewModel(app);
        return View(vm);
    }

    // =====================================================================
    // REPORT  ← FIX: أضفنا action اسمه Report عام + الـ 3 المخصصة
    // =====================================================================

    // مسار عام: /ApplicationsCenter/Report?status=Pending&faculty=...
    [HttpGet]
    [RequirePermission("Applications.View", "CanView")]
    public async Task<IActionResult> Report(string? status = null, string? faculty = null, int? cityId = null)
    {
        string title = status switch
        {
            "Accepted" => "الطلاب المقبولون",
            "Rejected" => "الطلاب المرفوضون",
            "Pending" => "الطلبات المعلقة",
            "UnderReview" => "الطلبات قيد المراجعة",
            "Waitlist" => "قائمة الانتظار",
            "Returned" => "الطلبات المعادة للتصحيح",
            _ => "تقرير الطلبات"
        };

        return await BuildReport(status, faculty, cityId, title);
    }

    [HttpGet]
    [RequirePermission("Applications.View", "CanView")]
    public async Task<IActionResult> ReportAccepted()
        => await BuildReport("Accepted", null, null, "الطلاب المقبولون");

    [HttpGet]
    [RequirePermission("Applications.View", "CanView")]
    public async Task<IActionResult> ReportRejected()
        => await BuildReport("Rejected", null, null, "الطلاب المرفوضون");

    [HttpGet]
    [RequirePermission("Applications.View", "CanView")]
    public async Task<IActionResult> ReportPending()
        => await BuildReport("Pending", null, null, "الطلبات المعلقة");

    // =====================================================================
    // REVIEW
    // =====================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Applications.Review", "CanEdit")]
    public async Task<IActionResult> Review(int id, ReviewDecisionViewModel model)
    {
        var app = await _db.Applications.Include(a => a.Student).FirstOrDefaultAsync(a => a.ID == id);
        if (app == null) return NotFound();

        if (model.Decision == "Rejected" && string.IsNullOrWhiteSpace(model.RejectionReason))
        {
            TempData["Error"] = "سبب الرفض إلزامي";
            return RedirectToAction("Details", new { id });
        }

        var oldStatus = app.Status;
        app.Status = model.Decision;
        app.ReviewedBy = CurrentUserId;
        app.ReviewedAt = DateTime.UtcNow;
        app.RejectionReason = model.Decision == "Rejected" ? model.RejectionReason : null;
        app.AdminNotes = model.AdminNotes;
        app.LastUpdatedAt = DateTime.UtcNow;
        app.LastUpdatedBy = CurrentUserId;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Staff",
            $"Application.{model.Decision}", "Application", id,
            new { Status = oldStatus }, new { Status = model.Decision });

        if (app.Student != null && !string.IsNullOrEmpty(app.Student.Email))
        {
            string subject = model.Decision switch
            {
                "Accepted" => "تهانينا! تم قبول طلبك - UniStay",
                "Rejected" => "نتيجة مراجعة طلب السكن - UniStay",
                "Returned" => "طلبك بحاجة إلى تعديل - UniStay",
                _ => "تحديث حالة الطلب - UniStay"
            };
            string body = model.Decision switch
            {
                "Accepted" => $"<h3>تهانينا!</h3><p>عزيزي {app.Student.FullName}، تم قبول طلب السكن الخاص بك.</p>",
                "Rejected" => $"<h3>نأسف</h3><p>عزيزي {app.Student.FullName}، تم رفض طلب السكن الخاص بك.</p><p>السبب: {model.RejectionReason}</p>",
                "Returned" => $"<h3>طلب تعديل</h3><p>عزيزي {app.Student.FullName}، طلبك بحاجة إلى تعديل. يرجى مراجعة الحساب الخاص بك.</p><p>ملاحظات: {model.AdminNotes}</p>",
                _ => $"<p>عزيزي {app.Student.FullName}، تم تحديث حالة طلبك إلى: {model.Decision}</p>"
            };
            var emailType = model.Decision == "Accepted" ? EmailType.ApplicationAccepted : EmailType.ApplicationRejected;
            await _email.SendAsync(app.Student.Email, subject, body, emailType, app.Student.ID);
        }

        TempData["Success"] = model.Decision switch
        {
            "Accepted" => "تم قبول الطلب بنجاح",
            "Rejected" => "تم رفض الطلب",
            "Returned" => "تم إعادة الطلب للتصحيح",
            _ => $"تم تحديث الحالة إلى {model.Decision}"
        };
        return RedirectToAction("Index");
    }

    // =====================================================================
    // QUICK ACTION
    // =====================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Applications.Review", "CanEdit")]
    public async Task<IActionResult> QuickAction(int id, string action, string? reason = null)
    {
        var app = await _db.Applications.FindAsync(id);
        if (app == null) return NotFound();

        var oldStatus = app.Status;

        switch (action)
        {
            case "accept":
                app.Status = "Accepted";
                break;
            case "reject":
                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["Error"] = "سبب الرفض إلزامي";
                    return RedirectToAction("Index");
                }
                app.Status = "Rejected";
                app.RejectionReason = reason;
                break;
            case "review":
                app.Status = "UnderReview";
                break;
            case "return":
                app.Status = "Returned";
                break;
            default:
                TempData["Error"] = "إجراء غير معروف";
                return RedirectToAction("Index");
        }

        app.ReviewedBy = CurrentUserId;
        app.ReviewedAt = DateTime.UtcNow;
        app.LastUpdatedAt = DateTime.UtcNow;
        app.LastUpdatedBy = CurrentUserId;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Staff",
            $"Application.Quick{action}", "Application", id,
            new { Status = oldStatus }, new { Status = app.Status });

        TempData["Success"] = action switch
        {
            "accept" => "تم قبول الطلب",
            "reject" => "تم رفض الطلب",
            "review" => "تم تحويل الطلب للمراجعة",
            "return" => "تم إعادة الطلب للتصحيح",
            _ => ""
        };
        return RedirectToAction("Index");
    }

    // =====================================================================
    // VERIFY FROM SERVER
    // =====================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Applications.Manage", "CanEdit")]
    public async Task<IActionResult> VerifyFromServer(int id)
    {
        var app = await _db.Applications.Include(a => a.Student).FirstOrDefaultAsync(a => a.ID == id);
        if (app?.Student == null)
            return Json(new { success = false, message = "الطلب أو الطالب غير موجود" });

        var result = await _api.SearchByNationalIDAsync(app.Student.NationalID);

        app.ServerVerificationStatus = result.IsMatch ? "Verified"
            : result.Found ? "VerifiedWithDiff" : "NotFound";
        app.ServerVerificationAt = DateTime.UtcNow;
        app.ServerVerificationBy = CurrentUserId;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Staff",
            "Application.ServerVerify", "Application", id,
            null, new { result.Found, result.IsMatch });

        return Json(new { success = true, status = app.ServerVerificationStatus });
    }

    // =====================================================================
    // EXPORT EXCEL
    // =====================================================================
    [HttpGet]
    [RequirePermission("Applications.View", "CanView")]
    public async Task<IActionResult> ExportExcel(string? status = null, int? cityId = null, string? faculty = null)
    {
        var query = BuildExportQuery(status, cityId, faculty);
        var apps = await query.ToListAsync();

        var rows = apps.Select(a => new ApplicationRowViewModel
        {
            StudentName = a.Student?.FullName ?? "",
            NationalID = a.Student?.NationalID ?? "",
            Faculty = a.Student?.Faculty,
            CityName = a.DormitoryCity?.Name,
            StudentType = a.StudentType,
            Status = a.Status,
            StatusDisplay = MapStatus(a.Status),
            CreatedAt = a.CreatedAt ?? DateTime.Now
        }).ToList();

        var columns = new[] { "الاسم", "الرقم القومي", "الكلية", "المدينة", "نوع الطالب", "الحالة", "تاريخ التقديم" };
        var data = _export.ExportToExcel("التطبيقات", columns, rows, r => new object?[] {
            r.StudentName, r.NationalID, r.Faculty, r.CityName, r.StudentType, r.StatusDisplay, r.CreatedAt.ToString("yyyy-MM-dd")
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Applications.xlsx");
    }

    // =====================================================================
    // EXPORT PDF
    // =====================================================================
    [HttpGet]
    [RequirePermission("Applications.View", "CanView")]
    public async Task<IActionResult> ExportPdf(string? status = null, int? cityId = null, string? faculty = null)
    {
        var query = BuildExportQuery(status, cityId, faculty);
        var apps = await query.ToListAsync();

        var columns = new[] { "الاسم", "الرقم القومي", "الكلية", "المدينة", "الحالة" };
        var rows = apps.Select(a => new[] {
            a.Student?.FullName ?? "", a.Student?.NationalID ?? "",
            a.Student?.Faculty ?? "", a.DormitoryCity?.Name ?? "",
            MapStatus(a.Status)
        }).ToArray();

        var pdf = _export.ExportToPdf("تقرير التطبيقات", columns, rows);
        return File(pdf, "application/pdf", "Applications.pdf");
    }

    // =====================================================================
    // PRIVATE HELPERS
    // =====================================================================

    /// <summary>بناء الـ ViewModel المشترك بين Details و Print</summary>
    private ApplicationDetailViewModel BuildDetailViewModel(Application app)
    {
        return new ApplicationDetailViewModel
        {
            ID = app.ID,
            Status = app.Status,
            StudentType = app.StudentType,
            HousingType = app.HousingType,
            AcademicYear = app.AcademicYear,
            MealSubscription = app.MealSubscription,
            HasSpecialNeeds = app.HasSpecialNeeds,
            SpecialNeedsDescription = app.SpecialNeedsDescription,
            RejectionReason = app.RejectionReason,
            AdminNotes = app.AdminNotes,
            CoordinationScore = app.CoordinationScore,
            CoordinationRank = app.CoordinationRank,
            ServerVerificationStatus = app.ServerVerificationStatus,
            ServerVerificationAt = app.ServerVerificationAt,
            CreatedAt = app.CreatedAt,
            LastUpdatedAt = app.LastUpdatedAt,
            Student = app.Student != null ? new StudentInfoViewModel
            {
                ID = app.Student.ID,
                FullName = app.Student.FullName,
                NationalID = app.Student.NationalID,
                StudentCode = app.Student.StudentCode,
                Gender = app.Student.Gender,
                Faculty = app.Student.Faculty,
                Department = app.Student.Department,
                Phone = app.Student.Phone,
                Email = app.Student.Email,
                Governorate = app.Student.Governorate,
                City = app.Student.City,
                DistanceFromUniv = app.Student.DistanceFromUniv,
                GradePercentage = app.Student.GradePercentage,
                HasDisability = app.Student.HasDisability,
                IsOrphan = app.Student.IsOrphan,
                IsLowIncome = app.Student.IsLowIncome,
                HasFamilyAbroad = app.Student.HasFamilyAbroad,
                HasMedicalCondition = app.Student.HasMedicalCondition,
                IsForeign = app.Student.IsForeign
            } : new StudentInfoViewModel(),
            DormitoryCity = new CityInfoViewModel { ID = app.DormitoryCity.ID, Name = app.DormitoryCity.Name },
            ReviewedBy = app.ReviewedByNavigation != null ? new ReviewInfoViewModel
            {
                Name = app.ReviewedByNavigation.Name,
                ReviewedAt = app.ReviewedAt
            } : null,
            Allocation = app.Allocation != null ? new AllocationInfoViewModel
            {
                ID = app.Allocation.ID,
                BuildingName = app.Allocation.CityRoom?.CityBuilding?.BuildingName,
                RoomNumber = app.Allocation.CityRoom?.RoomNumber,
                BedNumber = app.Allocation.BedNumber,
                Status = app.Allocation.Status
            } : null,
            Guardians = app.Student?.Guardians.Select(g => new GuardianInfoViewModel
            {
                FullName = g.FullName,
                GuardianType = g.GuardianType,
                Phone = g.Phone,
                Job = g.Job
            }).ToList() ?? new()
        };
    }

    /// <summary>بناء Report مع دعم فلترة اختيارية</summary>
    private async Task<IActionResult> BuildReport(string? status, string? faculty, int? cityId, string title)
    {
        var query = _db.Applications
            .Include(a => a.Student).Include(a => a.DormitoryCity)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);
        if (!string.IsNullOrEmpty(faculty))
            query = query.Where(a => a.Student!.Faculty == faculty);
        if (cityId.HasValue)
            query = query.Where(a => a.DormitoryCityID == cityId.Value);

        var apps = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ApplicationRowViewModel
            {
                ID = a.ID,
                StudentName = a.Student!.FullName,
                NationalID = a.Student.NationalID,
                Faculty = a.Student.Faculty,
                CityName = a.DormitoryCity.Name,
                StudentType = a.StudentType,
                Status = a.Status,
                StatusDisplay = MapStatus(a.Status),
                CreatedAt = a.CreatedAt!.Value
            })
            .ToListAsync();

        var vm = new ApplicationReportViewModel
        {
            Applications = apps,
            ReportTitle = title,
            TotalCount = apps.Count
        };
        return View("Report", vm);
    }

    private IQueryable<Application> BuildExportQuery(string? status, int? cityId, string? faculty)
    {
        var query = _db.Applications.Include(a => a.Student).Include(a => a.DormitoryCity).AsQueryable();
        if (!string.IsNullOrEmpty(status) && status != "All")
            query = query.Where(a => a.Status == status);
        if (cityId.HasValue)
            query = query.Where(a => a.DormitoryCityID == cityId.Value);
        if (!string.IsNullOrEmpty(faculty))
            query = query.Where(a => a.Student!.Faculty == faculty);
        return query;
    }

    private static string MapStatus(string status) => status switch
    {
        "Pending" => "معلق",
        "UnderReview" => "قيد المراجعة",
        "Accepted" => "مقبول",
        "Rejected" => "مرفوض",
        "Waitlist" => "قائمة انتظار",
        "Returned" => "معاد للتصحيح",
        _ => status
    };
}