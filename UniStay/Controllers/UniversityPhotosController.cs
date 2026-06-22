using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Admin;

namespace UniStay.Controllers;

[Route("UniversityPhotos")]
[AdminAuthorize]
public class UniversityPhotosController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly IAuditService _audit;
    private readonly IWebHostEnvironment _env;

    public UniversityPhotosController(
        AssuitDbContext db,
        IAuditService audit,
        IWebHostEnvironment env)
    {
        _db = db;
        _audit = audit;
        _env = env;
    }

    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    [HttpGet("Index")]
    [RequirePermission("Photos.Manage", "CanView")]
    public async Task<IActionResult> Index()
    {
        var photos = await _db.UniversityPhotos
            .Include(p => p.DormitoryCity)
            .OrderBy(p => p.SortOrder ?? 0)
            .ThenBy(p => p.Title)
            .Select(p => new UniversityPhotoViewModel
            {
                ID = p.ID,
                Title = p.Title,
                PhotoType = p.PhotoType,
                FilePath = p.FilePath,
                SortOrder = p.SortOrder,
                IsActive = p.IsActive ?? true,
                CityName = p.DormitoryCity != null ? p.DormitoryCity.Name : null
            })
            .ToListAsync();

        ViewBag.Cities = await _db.DormitoryCities
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(photos);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [RequirePermission("Photos.Manage", "CanCreate")]
    public async Task<IActionResult> Create(CreateUniversityPhotoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "بيانات غير صحيحة";
            return RedirectToAction("Index");
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "university-photos");
        if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.File.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await model.File.CopyToAsync(stream);
        }

        var photo = new UniversityPhoto
        {
            Title = model.Title,
            PhotoType = model.PhotoType ?? "Campus",
            FilePath = $"/uploads/university-photos/{fileName}",
            DormitoryCityID = model.DormitoryCityID,
            SortOrder = (byte?)(model.SortOrder ?? 0),
            IsActive = true
        };

        _db.UniversityPhotos.Add(photo);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Admin", "UniversityPhoto.Create", "UniversityPhoto", photo.ID,
            null, new { photo.Title, photo.PhotoType, photo.FilePath });

        TempData["Success"] = "تم إضافة الصورة بنجاح";
        return RedirectToAction("Index");
    }

    [HttpPost("Edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission("Photos.Manage", "CanEdit")]
    public async Task<IActionResult> Edit(EditUniversityPhotoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "بيانات غير صحيحة";
            return RedirectToAction("Index");
        }

        var photo = await _db.UniversityPhotos.FindAsync(model.ID);
        if (photo == null) return NotFound();

        photo.Title = model.Title;
        photo.PhotoType = model.PhotoType ?? "Campus";
        photo.DormitoryCityID = model.DormitoryCityID;
        photo.SortOrder = (byte?)(model.SortOrder ?? 0);
        photo.IsActive = model.IsActive;

        if (model.File != null)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "university-photos");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.File.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            if (!string.IsNullOrEmpty(photo.FilePath))
            {
                var oldPath = Path.Combine(_env.WebRootPath, photo.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            photo.FilePath = $"/uploads/university-photos/{fileName}";
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Admin", "UniversityPhoto.Edit", "UniversityPhoto", model.ID,
            null, new { model.Title, model.IsActive });

        TempData["Success"] = "تم تحديث الصورة بنجاح";
        return RedirectToAction("Index");
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    [RequirePermission("Photos.Manage", "CanDelete")]
    public async Task<IActionResult> Delete(int id)
    {
        var photo = await _db.UniversityPhotos.FindAsync(id);
        if (photo == null) return Json(new { success = false, message = "الصورة غير موجودة" });

        if (!string.IsNullOrEmpty(photo.FilePath))
        {
            var oldPath = Path.Combine(_env.WebRootPath, photo.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        _db.UniversityPhotos.Remove(photo);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(CurrentUserId, "Admin", "UniversityPhoto.Delete", "UniversityPhoto", id,
            null, new { photo.Title });

        return Json(new { success = true, message = "تم حذف الصورة" });
    }
}
