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
    public async Task<IActionResult> Index(string? search = null, int? cityId = null, string? docStatus = null, int page = 1)
    {
        const int pageSize = 30;

        var query = _db.Applications
            .Include(a => a.Student)
            .Include(a => a.DormitoryCity)
            .Include(a => a.Documents)
            .Where(a => a.Status != "Accepted" && a.Status != "Rejected")
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(a => a.Student!.FullName.Contains(search) || a.Student.NationalID.Contains(search));
        if (cityId.HasValue)
            query = query.Where(a => a.DormitoryCityID == cityId.Value);

        var total = await query.CountAsync();
        var apps = await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var rows = apps.Select(a =>
        {
            var docs = a.Documents.ToList();
            return new OnlineReviewRowViewModel
            {
                ApplicationID = a.ID, StudentName = a.Student?.FullName ?? "",
                NationalID = a.Student?.NationalID ?? "", Faculty = a.Student?.Faculty,
                CityName = a.DormitoryCity?.Name, Status = a.Status,
                SubmittedAt = a.CreatedAt, TotalDocs = docs.Count,
                VerifiedDocs = docs.Count(d => d.IsVerified == true),
                RejectedDocs = docs.Count(d => d.IsVerified == false),
                PendingDocs = docs.Count(d => d.IsVerified == null)
            };
        }).ToList();

        if (!string.IsNullOrEmpty(docStatus))
        {
            rows = docStatus switch
            {
                "verified" => rows.Where(r => r.AllDocumentsVerified).ToList(),
                "pending" => rows.Where(r => !r.AllDocumentsVerified && !r.HasMissingDocs).ToList(),
                "missing" => rows.Where(r => r.HasMissingDocs).ToList(),
                _ => rows
            };
        }

        var all = await _db.Applications.Include(a => a.Documents)
            .Where(a => a.Status != "Accepted" && a.Status != "Rejected").ToListAsync();

        var vm = new OnlineReviewIndexViewModel
        {
            Applications = rows, Filter = new OnlineReviewFilterViewModel { Search = search, CityID = cityId, DocumentStatus = docStatus },
            TotalCount = total, Page = page, TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            PendingReview = all.Count(a => a.Status == "Pending"),
            DocumentsVerified = all.Count(a => a.Documents.Any(d => d.IsVerified == true)),
            DocumentsRejected = all.Count(a => a.Documents.Any(d => d.IsVerified == false)),
            MissingDocuments = all.Count(a => !a.Documents.Any())
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
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.ID == id);

        if (app == null) return NotFound();

        var vm = new OnlineReviewDetailViewModel
        {
            ApplicationID = app.ID, StudentName = app.Student?.FullName ?? "",
            NationalID = app.Student?.NationalID ?? "", Faculty = app.Student?.Faculty,
            CityName = app.DormitoryCity?.Name, Status = app.Status,
            AdminNotes = app.AdminNotes,
            Documents = app.Documents.Select(d => new DocumentReviewViewModel
            {
                DocumentID = d.ID, DocumentType = d.DocumentType,
                FileName = d.FileName, FilePath = d.FilePath,
                IsVerified = d.IsVerified, UploadedAt = d.UploadedAt
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Applications.Review", "CanEdit")]
    public async Task<IActionResult> VerifyDocument(int docId, bool verified, string? notes = null)
    {
        var doc = await _db.Documents.Include(d => d.Application).FirstOrDefaultAsync(d => d.ID == docId);
        if (doc == null) return Json(new { success = false, message = "المستند غير موجود" });

        doc.IsVerified = verified;
        doc.VerifiedBy = CurrentUserId;
        doc.VerifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Staff",
            $"Document.{(verified ? "Approve" : "Reject")}", "Document", docId,
            null, new { doc.DocumentType, Verified = verified });

        // Check if all documents are verified
        var appId = doc.ApplicationID;
        var allDocs = await _db.Documents.Where(d => d.ApplicationID == appId).ToListAsync();
        var allVerified = allDocs.All(d => d.IsVerified == true);
        var anyRejected = allDocs.Any(d => d.IsVerified == false);

        return Json(new { success = true, allVerified, anyRejected, verifiedCount = allDocs.Count(d => d.IsVerified == true), totalCount = allDocs.Count });
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
    public async Task<IActionResult> RequestAdditionalDocuments(int id, string? message = null)
    {
        var app = await _db.Applications.Include(a => a.Student).FirstOrDefaultAsync(a => a.ID == id);
        if (app == null) return NotFound();

        if (app.Student?.Email != null)
            await _email.SendAsync(app.Student.Email, "مطلوب مستندات إضافية - UniStay",
                $"<h3>مستندات إضافية</h3><p>عزيزي {app.Student.FullName}، يرجى تقديم مستندات إضافية لاستكمال طلبك.</p>{(message != null ? $"<p>{message}</p>" : "")}",
                EmailType.General, app.StudentID);

        await _audit.LogAsync(CurrentUserId, "Staff", "Application.RequestDocs", "Application", id,
            null, new { Message = message });

        TempData["Success"] = "تم طلب المستندات الإضافية";
        return RedirectToAction("Index");
    }

    [HttpGet]
    [RequirePermission("Applications.View", "CanView")]
    public async Task<IActionResult> ExportExcel()
    {
        var apps = await _db.Applications.Include(a => a.Student).Include(a => a.DormitoryCity).Include(a => a.Documents)
            .Where(a => a.Status != "Accepted" && a.Status != "Rejected")
            .OrderByDescending(a => a.CreatedAt).ToListAsync();

        var columns = new[] { "الاسم", "الرقم القومي", "الكلية", "المدينة", "الحالة", "المستندات", "موثق", "تاريخ التقديم" };
        var data = _export.ExportToExcel("مراجعة الطلبات", columns, apps, a => new object?[] {
            a.Student?.FullName, a.Student?.NationalID, a.Student?.Faculty,
            a.DormitoryCity?.Name, a.Status, a.Documents.Count,
            a.Documents.Count(d => d.IsVerified == true), a.CreatedAt?.ToString("yyyy-MM-dd")
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OnlineReview.xlsx");
    }
}
