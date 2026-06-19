using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Photos;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class PhotosController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly IPhotoService _photoService;
    private readonly IAuditService _audit;
    private readonly IReportExportService _export;

    public PhotosController(AssuitDbContext db, IPhotoService photoService, IAuditService audit, IReportExportService export)
    {
        _db = db;
        _photoService = photoService;
        _audit = audit;
        _export = export;
    }

    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [HttpGet]
    [RequirePermission("Photos.View", "CanView")]
    public async Task<IActionResult> Index(string? search = null, int? cityId = null, string? photoStatus = null, int page = 1)
    {
        const int pageSize = 30;

        var query = _db.Students
            .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                .ThenInclude(a => a.CityRoom).ThenInclude(cr => cr.CityBuilding).ThenInclude(cb => cb.DormitoryCity)
            .Where(s => s.IsDeleted != true)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.FullName.Contains(search) || s.NationalID.Contains(search));

        if (cityId.HasValue)
            query = query.Where(s => s.Allocations.Any(a => a.CityRoom.CityBuilding.DormitoryCityID == cityId.Value));

        if (!string.IsNullOrEmpty(photoStatus))
        {
            query = photoStatus switch
            {
                "with" => query.Where(s => s.Photo != null && s.Photo != ""),
                "without" => query.Where(s => s.Photo == null || s.Photo == ""),
                _ => query
            };
        }

        var total = await query.CountAsync();
        var students = await query.OrderByDescending(s => s.ID)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var rows = students.Select(s =>
        {
            var alloc = s.Allocations.FirstOrDefault(a => a.Status == "Active");
            return new StudentPhotoRowViewModel
            {
                StudentID = s.ID,
                FullName = s.FullName,
                NationalID = s.NationalID,
                Faculty = s.Faculty,
                CityName = alloc?.CityRoom?.CityBuilding?.DormitoryCity?.Name,
                PhotoPath = s.Photo
            };
        }).ToList();

        var withPhotos = await _db.Students.CountAsync(s => s.Photo != null && s.Photo != "" && s.IsDeleted != true);
        var withoutPhotos = await _db.Students.CountAsync(s => (s.Photo == null || s.Photo == "") && s.IsDeleted != true);

        var cities = await _db.DormitoryCities.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

        var vm = new PhotoIndexViewModel
        {
            Students = rows,
            Filter = new PhotoFilterViewModel { Search = search, CityID = cityId, PhotoStatus = photoStatus },
            TotalCount = total,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            WithPhotoCount = withPhotos,
            WithoutPhotoCount = withoutPhotos,
            Cities = cities.Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Photos.Manage", "CanEdit")]
    public async Task<IActionResult> Upload(int id)
    {
        var student = await _photoService.GetStudentPhotoInfoAsync(id);
        if (student == null) return NotFound();

        return View(student);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Photos.Manage", "CanEdit")]
    public async Task<IActionResult> Upload(int id, UploadPhotoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "يرجى اختيار ملف صورة صالح";
            return RedirectToAction("Upload", new { id });
        }

        try
        {
            var path = await _photoService.UploadPhotoAsync(id, model.PhotoFile, CurrentUserId);
            if (path == null) return NotFound();

            TempData["Success"] = "تم رفع الصورة بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Upload", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Photos.Manage", "CanEdit")]
    public async Task<IActionResult> DeletePhoto(int id)
    {
        var deleted = await _photoService.DeletePhotoAsync(id, CurrentUserId);
        if (!deleted) return Json(new { success = false, message = "لم يتم العثور على الصورة" });

        return Json(new { success = true });
    }

    [HttpGet]
    [RequirePermission("Photos.Manage", "CanEdit")]
    public IActionResult BulkImport()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Photos.Manage", "CanEdit")]
    public async Task<IActionResult> BulkImport(BulkImportViewModel model)
    {
        if (!ModelState.IsValid) return View();

        if (!model.ZipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "يرجى رفع ملف ZIP";
            return View();
        }

        var result = await _photoService.BulkImportFromZipAsync(model.ZipFile, model.MatchBy ?? "StudentID", CurrentUserId);
        TempData["ImportResult"] = JsonSerializer.Serialize(result, _jsonOptions);
        return RedirectToAction("ImportResult");
    }

    [HttpGet]
    [RequirePermission("Photos.Manage", "CanView")]
    public IActionResult ImportResult()
    {
        var json = TempData["ImportResult"] as string;
        var model = json != null
            ? JsonSerializer.Deserialize<BulkImportResultViewModel>(json, _jsonOptions) ?? new BulkImportResultViewModel()
            : new BulkImportResultViewModel();
        return View(model);
    }

    [HttpGet]
    [RequirePermission("Photos.View", "CanView")]
    public async Task<IActionResult> ExportExcel()
    {
        var students = await _db.Students
            .Where(s => s.IsDeleted != true)
            .OrderByDescending(s => s.ID).ToListAsync();

        var columns = new[] { "الاسم", "الرقم القومي", "الكلية", "حالة الصورة", "رابط الصورة" };
        var data = _export.ExportToExcel("حالة الصور", columns, students, s => new object?[] {
            s.FullName, s.NationalID, s.Faculty,
            string.IsNullOrEmpty(s.Photo) ? "بدون صورة" : "يوجد صورة",
            s.Photo
        });

        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StudentPhotos.xlsx");
    }
}
