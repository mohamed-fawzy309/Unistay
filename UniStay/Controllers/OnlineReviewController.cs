using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Applications;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
public class OnlineReviewController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;
    private readonly IReportExportService _export;

    public OnlineReviewController(AssuitDbContext db, IAuditService audit, IEmailService email, IReportExportService export)
    {
        _db = db;
        _audit = audit;
        _email = email;
        _export = export;
    }

    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    [HttpGet]
    [RequirePermission("Applications.Review", "CanEdit")]
    public async Task<IActionResult> Index(string? search = null, int? cityId = null, int page = 1)
    {
        const int pageSize = 30;

        var query = _db.Applications
            .Include(a => a.Student)
            .Include(a => a.DormitoryCity)
            .Where(a => a.Status != "Accepted" && a.Status != "Rejected")
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(a => a.Student!.FullName.Contains(search) || a.Student.NationalID.Contains(search));
        if (cityId.HasValue)
            query = query.Where(a => a.DormitoryCityID == cityId.Value);

        var total = await query.CountAsync();
        var apps = await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var rows = apps.Select(a => new OnlineReviewRowViewModel
        {
            ApplicationID = a.ID, StudentName = a.Student?.FullName ?? "",
            NationalID = a.Student?.NationalID ?? "", Faculty = a.Student?.Faculty,
            CityName = a.DormitoryCity?.Name, Status = a.Status,
            ServerVerificationStatus = a.ServerVerificationStatus,
            SubmittedAt = a.CreatedAt
        }).ToList();

        var vm = new OnlineReviewIndexViewModel
        {
            Applications = rows, Filter = new OnlineReviewFilterViewModel { Search = search, CityID = cityId },
            TotalCount = total, Page = page, TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            PendingReview = await _db.Applications.CountAsync(a => a.Status == "Pending")
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Applications.Review", "CanEdit")]
    public async Task<IActionResult> Review(int id)
    {
        var app = await _db.Applications
            .Include(a => a.Student)
            .Include(a => a.DormitoryCity)
            .FirstOrDefaultAsync(a => a.ID == id);

        if (app == null) return NotFound();

        var vm = new OnlineReviewDetailViewModel
        {
            ApplicationID = app.ID, StudentName = app.Student?.FullName ?? "",
            NationalID = app.Student?.NationalID ?? "", Faculty = app.Student?.Faculty,
            CityName = app.DormitoryCity?.Name, Status = app.Status,
            AdminNotes = app.AdminNotes
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Applications.Review", "CanEdit")]
    public async Task<IActionResult> ApproveApplication(int id)
    {
        var app = await _db.Applications.Include(a => a.Student).FirstOrDefaultAsync(a => a.ID == id);
        if (app == null) return NotFound();

        var oldStatus = app.Status;
        app.Status = "Accepted";
        app.ReviewedBy = CurrentUserId;
        app.ReviewedAt = DateTime.UtcNow;
        app.LastUpdatedAt = DateTime.UtcNow;
        app.LastUpdatedBy = CurrentUserId;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Staff", "Application.Approve", "Application", id,
            new { Status = oldStatus }, new { Status = "Accepted" });

        if (app.Student?.Email != null)
            await _email.SendAsync(app.Student.Email, "تهانينا! تم قبول طلبك - UniStay",
                $"<h3>تهانينا!</h3><p>عزيزي {app.Student.FullName}، تم قبول طلب السكن الخاص بك.</p>",
                EmailType.ApplicationAccepted, app.StudentID);

        TempData["Success"] = "تم قبول الطلب بنجاح";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Applications.Review", "CanEdit")]
    public async Task<IActionResult> RejectApplication(int id, string? reason = null)
    {
        var app = await _db.Applications.Include(a => a.Student).FirstOrDefaultAsync(a => a.ID == id);
        if (app == null) return NotFound();

        var oldStatus = app.Status;
        app.Status = "Rejected";
        app.RejectionReason = reason;
        app.ReviewedBy = CurrentUserId;
        app.ReviewedAt = DateTime.UtcNow;
        app.LastUpdatedAt = DateTime.UtcNow;
        app.LastUpdatedBy = CurrentUserId;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Staff", "Application.Reject", "Application", id,
            new { Status = oldStatus }, new { Status = "Rejected", Reason = reason });

        if (app.Student?.Email != null)
            await _email.SendAsync(app.Student.Email, "نتيجة مراجعة طلب السكن - UniStay",
                $"<h3>نأسف</h3><p>عزيزي {app.Student.FullName}، تم رفض طلب السكن الخاص بك.</p>{(reason != null ? $"<p>السبب: {reason}</p>" : "")}",
                EmailType.ApplicationRejected, app.StudentID);

        TempData["Success"] = "تم رفض الطلب";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Applications.Review", "CanEdit")]
    public async Task<IActionResult> VerifyAll()
    {
        var apps = await _db.Applications
            .Where(a => a.Status == "Pending" && a.ServerVerificationStatus != "Verified")
            .ToListAsync();

        if (!apps.Any())
        {
            TempData["Warning"] = "لا توجد طلبات قيد المراجعة للتحقق منها";
            return RedirectToAction("Index");
        }

        var now = DateTime.UtcNow;
        var userId = CurrentUserId;

        foreach (var app in apps)
        {
            app.ServerVerificationStatus = "Verified";
            app.ServerVerificationAt = now;
            app.ServerVerificationBy = userId;
            app.LastUpdatedAt = now;
            app.LastUpdatedBy = userId;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "Staff", "Application.VerifyAll", "Application", null,
            new { Count = apps.Count, OldStatus = "Pending" },
            new { Count = apps.Count, NewStatus = "Verified" });

        TempData["Success"] = $"تم التحقق من {apps.Count} طلب بنجاح";
        return RedirectToAction("Index");
    }

    [HttpGet]
    [RequirePermission("Applications.View", "CanView")]
    public async Task<IActionResult> ExportExcel()
    {
        var apps = await _db.Applications.Include(a => a.Student).Include(a => a.DormitoryCity)
            .Where(a => a.Status != "Accepted" && a.Status != "Rejected")
            .OrderByDescending(a => a.CreatedAt).ToListAsync();

        var columns = new[] { "الاسم", "الرقم القومي", "الكلية", "المدينة", "الحالة", "تاريخ التقديم" };
        var data = _export.ExportToExcel("مراجعة الطلبات", columns, apps, a => new object?[] {
            a.Student?.FullName, a.Student?.NationalID, a.Student?.Faculty,
            a.DormitoryCity?.Name, a.Status, a.CreatedAt?.ToString("yyyy-MM-dd")
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OnlineReview.xlsx");
    }
}
