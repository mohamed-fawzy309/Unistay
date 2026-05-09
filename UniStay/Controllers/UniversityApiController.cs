using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace UniStay.Controllers;

[Route("[controller]")]
[StaffAuthorize]
public class UniversityApiController : Controller
{
    private readonly AssuitDbContext _db; // <-- تغيير لـ AssuitDbContext
    private readonly IUniversityApiService _apiService;
    private readonly IAuditService _audit;

    public UniversityApiController(
        AssuitDbContext db, // <-- هنا كمان
        IUniversityApiService apiService,
        IAuditService audit)
    {
        _db = db;
        _apiService = apiService;
        _audit = audit;
    }

    private int CurrentUserId
    {
        get
        {
            var c = User.FindFirst("UserID")?.Value;
            return int.TryParse(c, out var id) ? id : 0;
        }
    }

    // ========== 1. VERIFY STUDENT ==========
    [HttpPost("VerifyStudent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyStudent([FromBody] VerifyStudentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NationalID) || request.NationalID.Length != 14)
            return BadRequest(new { success = false, message = "الرقم القومي يجب أن يكون 14 رقماً" });

        var student = await _db.Students
            .Include(s => s.Applications)
            .FirstOrDefaultAsync(s => s.NationalID == request.NationalID);

        if (student == null)
            return NotFound(new { success = false, message = "الطالب غير موجود في النظام المحلي" });

        // استدعاء API
        var apiResult = await _apiService.SearchByNationalIDAsync(request.NationalID);

        // تحديث حالة الطلب
        var application = student.Applications
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefault(a => a.AcademicYear == GetCurrentAcademicYear());

        if (application != null)
        {
            application.ServerVerificationStatus = apiResult.IsMatch ? "Verified" :
                                                  (apiResult.Found ? "VerifiedWithDiff" : "NotFound");
            application.ServerVerificationAt = DateTime.Now;
            application.ServerVerificationBy = CurrentUserId;
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync(
            CurrentUserId,
            "Staff",
            "UniversityApi.VerifyStudent",
            "Student",
            student.ID,
            null,
            new { apiResult.Found, apiResult.IsMatch },
            null,
            application?.DormitoryCityID);

        return Ok(new
        {
            success = true,
            apiData = apiResult,
            localData = new
            {
                student.FullName,
                student.Faculty,
                student.AcademicYear,
                student.GradePercentage,
                student.IsEnrolled
            },
            comparison = apiResult.Differences
        });
    }

    // ========== 2. SEARCH STAFF ==========
    [HttpPost("SearchStaff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchStaff([FromBody] SearchStaffRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NationalID))
            return BadRequest(new { success = false, message = "ادخل الرقم القومي" });

        var result = await _apiService.SearchStaffByNationalIDAsync(request.NationalID);

        if (!result.Found)
            return Ok(new { success = false, message = "الموظف غير موجود في سجلات الجامعة" });

        // التحقق من عدم التكرار
        var exists = await _db.SystemUsers
            .AnyAsync(u => u.NationalID == request.NationalID && u.IsDeleted != true);

        if (exists)
            return Ok(new { success = false, message = "هذا الموظف مسجل مسبقاً" });

        return Ok(new
        {
            success = true,
            data = new
            {
                result.FullName,
                result.JobTitle,
                result.Found
            }
        });
    }

    // ========== 3. BULK VALIDATE ==========
    [HttpGet("BulkValidate")]
    public IActionResult BulkValidate()
    {
        return View(new BulkValidateViewModel());
    }

    [HttpPost("BulkValidate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkValidate(BulkValidateViewModel vm)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(vm.NationalIDsText))
        {
            ModelState.AddModelError("", "ادخل أرقام قومية صحيحة");
            return View(vm);
        }

        var ids = vm.NationalIDsText
            .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length == 14 && s.All(char.IsDigit))
            .Distinct()
            .ToList();

        if (!ids.Any())
        {
            ModelState.AddModelError("", "لا يوجد أرقام قومية صحيحة");
            return View(vm);
        }

        var results = await _apiService.BulkValidateAsync(ids);

        // تفاصيل كل رقم - استخدم UniversityAPISyncs (صح)
        var details = new List<ValidationDetailViewModel>();
        foreach (var id in ids)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.NationalID == id);
            var sync = await _db.UniversityAPISyncs  // <-- تصحيح الاسم هنا
                .Where(s => s.NationalID == id)
                .OrderByDescending(s => s.SyncedAt)
                .FirstOrDefaultAsync();

            details.Add(new ValidationDetailViewModel
            {
                NationalID = id,
                LocalExists = student != null,
                ServerFound = sync != null,
                IsMatch = sync?.IsMatch == true,
                LastSync = sync?.SyncedAt,
                Status = sync?.IsMatch == true ? "مطابق" : (sync != null ? "اختلافات" : "غير متحقق")
            });
        }

        ViewBag.Results = results;
        ViewBag.Details = details;
        ViewBag.TotalCount = ids.Count;
        ViewBag.ValidCount = details.Count(d => d.IsMatch);

        await _audit.LogAsync(
            CurrentUserId,
            "Staff",
            "UniversityApi.BulkValidate",
            "UniversityAPISync",
            0,
            null,
            new { Count = ids.Count, Success = results.Success },
            null,
            null);

        return View("BulkValidateResults", vm);
    }

    // ========== 4. VALIDATION LOG ==========
    [HttpGet("ValidationLog")]
    public async Task<IActionResult> ValidationLog(
        string? nationalId = null,
        string? syncType = null,
        bool? isMatch = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1)
    {
        // <-- استخدم UniversityAPISyncs (بـ s مش es) وSyncedByNavigation
        var query = _db.UniversityAPISyncs
            .Include(s => s.SyncedByNavigation) // <-- Navigation property الصح
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(nationalId))
            query = query.Where(s => s.NationalID == nationalId);

        if (!string.IsNullOrWhiteSpace(syncType))
            query = query.Where(s => s.SyncType == syncType);

        if (isMatch.HasValue)
            query = query.Where(s => s.IsMatch == isMatch.Value);

        if (from.HasValue)
            query = query.Where(s => s.SyncedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.SyncedAt <= to.Value.AddDays(1));

        var logs = await query
            .OrderByDescending(s => s.SyncedAt)
            .Skip((page - 1) * 30)
            .Take(30)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.HasMore = await query.Skip(page * 30).AnyAsync();
        ViewBag.Filters = new { nationalId, syncType, isMatch, from, to };

        return View(logs);
    }

    // ========== AJAX Helper ==========
    [HttpGet("GetComparison/{syncId}")]
    public async Task<IActionResult> GetComparison(int syncId)
    {
        var sync = await _db.UniversityAPISyncs.FindAsync(syncId); // <-- UniversityAPISyncs
        if (sync == null) return NotFound();

        return Ok(new
        {
            local = JsonConvert.DeserializeObject(sync.LocalData ?? "{}"),
            server = JsonConvert.DeserializeObject(sync.APIData ?? "{}"),
            differences = JsonConvert.DeserializeObject(sync.DifferenceDetails ?? "{}")
        });
    }

    private string GetCurrentAcademicYear()
    {
        var year = DateTime.Now.Year;
        return DateTime.Now.Month >= 9 ? $"{year}-{year + 1}" : $"{year - 1}-{year}";
    }
}

// ========== ViewModels ==========
public class VerifyStudentRequest
{
    [Required]
    [StringLength(14, MinimumLength = 14)]
    public string NationalID { get; set; } = null!;
    public int? ApplicationId { get; set; }
}

public class SearchStaffRequest
{
    [Required]
    public string NationalID { get; set; } = null!;
}

public class BulkValidateViewModel
{
    [Display(Name = "أرقام قومية (واحد في كل سطر أو مفصول بفاصلة)")]
    public string NationalIDsText { get; set; } = null!;
}

public class ValidationDetailViewModel
{
    public string NationalID { get; set; } = null!;
    public bool LocalExists { get; set; }
    public bool ServerFound { get; set; }
    public bool IsMatch { get; set; }
    public DateTime? LastSync { get; set; }
    public string Status { get; set; } = null!;
}