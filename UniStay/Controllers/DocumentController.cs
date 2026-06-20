using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Document;

namespace UniStay.Controllers
{
    [Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
    public class DocumentController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;

        public DocumentController(AssuitDbContext db, IAuditService audit, IEmailService email)
        {
            _db = db;
            _audit = audit;
            _email = email;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        [HttpGet]
        public async Task<IActionResult> AdminIndex(string? filterStatus = null, int page = 1)
        {
            var query = _db.Documents
                .Include(d => d.Student)
                .Include(d => d.VerifiedByNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "All")
            {
                if (filterStatus == "Verified")
                    query = query.Where(d => d.IsVerified == true);
                else if (filterStatus == "Pending")
                    query = query.Where(d => d.IsVerified != true);
            }

            var total = await query.CountAsync();

            var documents = await query
                .OrderByDescending(d => d.UploadedAt)
                .Skip((page - 1) * 20)
                .Take(20)
                .Select(d => new DocumentRowViewModel
                {
                    ID = d.ID,
                    StudentID = d.StudentID,
                    StudentName = d.Student.FullName,
                    NationalID = d.Student.NationalID,
                    DocumentType = d.DocumentType,
                    FileName = d.FileName,
                    IsVerified = d.IsVerified,
                    UploadedAt = d.UploadedAt,
                    VerifiedByName = d.VerifiedByNavigation != null ? d.VerifiedByNavigation.Name : null
                })
                .ToListAsync();

            ViewBag.Students = await _db.Students
                .Where(s => s.IsDeleted != true)
                .OrderBy(s => s.FullName)
                .Select(s => new { s.ID, s.FullName, s.NationalID })
                .ToListAsync();

            var vm = new DocumentAdminIndexViewModel
            {
                Documents = documents,
                FilterStatus = filterStatus,
                Page = page,
                TotalPages = (int)Math.Ceiling(total / 20.0)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadDocumentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("AdminIndex");
            }

            if (model.File == null || model.File.Length == 0)
            {
                TempData["Error"] = "يرجى اختيار ملف";
                return RedirectToAction("AdminIndex");
            }

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(model.File.FileName);
            var safeName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsDir, safeName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            var document = new Document
            {
                StudentID = model.StudentID,
                ApplicationID = model.ApplicationID,
                DocumentType = model.DocumentType,
                FileName = model.File.FileName,
                FilePath = $"/uploads/documents/{safeName}",
                UploadedAt = DateTime.UtcNow
            };

            _db.Documents.Add(document);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Document.Upload", "Document", document.ID,
                null, new { document.StudentID, document.DocumentType, document.FileName });

            TempData["Success"] = "تم رفع المستند بنجاح";
            return RedirectToAction("AdminIndex");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(int id)
        {
            var document = await _db.Documents.FindAsync(id);
            if (document == null)
                return Json(new { success = false, message = "المستند غير موجود" });

            if (document.IsVerified == true)
                return Json(new { success = false, message = "المستند موثق بالفعل" });

            document.IsVerified = true;
            document.VerifiedBy = CurrentUserId;
            document.VerifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Document.Verify", "Document", id,
                new { IsVerified = false }, new { IsVerified = true });

            return Json(new { success = true, message = "تم توثيق المستند بنجاح" });
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var document = await _db.Documents.FindAsync(id);
            if (document == null || string.IsNullOrEmpty(document.FilePath))
                return NotFound();

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(document.FilePath);
            var contentType = ext?.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".doc" or ".docx" => "application/msword",
                _ => "application/octet-stream"
            };

            return File(new FileStream(fullPath, FileMode.Open, FileAccess.Read), contentType, document.FileName ?? Path.GetFileName(fullPath));
        }
    }
}
