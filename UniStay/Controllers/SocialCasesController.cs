using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Helpers;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.SocialCases;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
[RequirePermission("SocialCases.Manage")]
public class SocialCasesController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly IAuditService _audit;
    private readonly IReportExportService _export;
    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    public SocialCasesController(AssuitDbContext db, IAuditService audit, IReportExportService export)
    {
        _db = db;
        _audit = audit;
        _export = export;
    }

    public async Task<IActionResult> Index(string search, string status, string priority, string caseType, int page = 1)
    {
        const int pageSize = 20;

        IQueryable<SocialCase> query = _db.SocialCases.Include(sc => sc.Student);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(sc =>
                sc.Student.FullName.Contains(term) ||
                sc.Student.StudentCode.Contains(term) ||
                (sc.Description != null && sc.Description.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(sc => sc.Status == status);

        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(sc => sc.Priority == priority);

        if (!string.IsNullOrWhiteSpace(caseType))
            query = query.Where(sc => sc.CaseType == caseType);

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        var cases = await query
            .OrderByDescending(sc => sc.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sc => new SocialCaseListItem
            {
                CaseId = sc.ID,
                StudentName = sc.Student.FullName,
                StudentCode = sc.Student.StudentCode,
                CaseType = sc.CaseType,
                Priority = sc.Priority,
                Status = sc.Status,
                AssignedTo = sc.AssignedTo,
                CreatedAt = sc.CreatedAt
            })
            .ToListAsync();

        var vm = new SocialCaseListVM
        {
            Cases = cases,
            SearchTerm = search,
            StatusFilter = status,
            PriorityFilter = priority,
            CaseTypeFilter = caseType,
            Page = page,
            TotalPages = totalPages
        };

        await _audit.LogAsync(CurrentUserId, "Staff", "SocialCases.Index", "SocialCase");
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var sc = await _db.SocialCases
            .Include(s => s.Student)
            .FirstOrDefaultAsync(s => s.ID == id);

        if (sc == null) return NotFound();

        var vm = new SocialCaseDetailsVM
        {
            CaseId = sc.ID,
            StudentId = sc.StudentID,
            StudentName = sc.Student.FullName,
            StudentCode = sc.Student.StudentCode,
            NationalID = sc.Student.NationalID,
            StudentPhone = sc.Student.Phone,
            CaseType = sc.CaseType,
            Description = sc.Description,
            Status = sc.Status,
            Priority = sc.Priority,
            AssignedTo = sc.AssignedTo,
            CreatedAt = sc.CreatedAt,
            ClosedAt = sc.ClosedAt
        };

        await _audit.LogAsync(CurrentUserId, "Staff", "SocialCases.Details", "SocialCase", id);
        return View(vm);
    }

    public IActionResult Create(int? studentId)
    {
        ViewBag.StudentId = studentId;
        return View(new SocialCaseCreateVM());
    }

    [HttpGet]
    public async Task<IActionResult> SearchStudents(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return Json(new List<object>());

        var results = await _db.Students
            .Where(s => s.FullName.Contains(term) ||
                        s.StudentCode.Contains(term) ||
                        (s.NationalID != null && s.NationalID.Contains(term)))
            .Take(10)
            .Select(s => new { id = s.ID, name = s.FullName, code = s.StudentCode, nationalId = s.NationalID })
            .ToListAsync();

        return Json(results);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SocialCaseCreateVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var student = await _db.Students.FindAsync(vm.StudentID);
        if (student == null)
        {
            ModelState.AddModelError("StudentID", "الطالب غير موجود");
            return View(vm);
        }

        var sc = new SocialCase
        {
            StudentID = vm.StudentID,
            CaseType = vm.CaseType,
            Description = vm.Description,
            Priority = vm.Priority ?? "متوسطة",
            AssignedTo = CurrentUserId,
            Status = "مفتوحة",
            CreatedAt = DateTime.Now
        };

        _db.SocialCases.Add(sc);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Staff", "SocialCases.Create", "SocialCase", sc.ID);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var sc = await _db.SocialCases.FindAsync(id);
        if (sc == null) return NotFound();

        var vm = new SocialCaseEditVM
        {
            CaseId = sc.ID,
            Status = sc.Status,
            Priority = sc.Priority
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SocialCaseEditVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var sc = await _db.SocialCases.FindAsync(vm.CaseId);
        if (sc == null) return NotFound();

        sc.Status = vm.Status ?? sc.Status;
        sc.Priority = vm.Priority ?? sc.Priority;

        if (vm.Status == "مغلقة" || vm.Status == "مؤرشفة")
            sc.ClosedAt ??= DateTime.Now;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Staff", "SocialCases.Edit", "SocialCase", vm.CaseId);

        return RedirectToAction(nameof(Details), new { id = vm.CaseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartInvestigation(int id)
    {
        var sc = await _db.SocialCases.FindAsync(id);
        if (sc == null) return NotFound();

        sc.Status = "قيد التحقيق";
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Staff", "SocialCases.StartInvestigation", "SocialCase", id);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var sc = await _db.SocialCases.FindAsync(id);
        if (sc == null) return NotFound();

        sc.Status = "مغلقة";
        sc.ClosedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Staff", "SocialCases.Close", "SocialCase", id);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReOpen(int id)
    {
        var sc = await _db.SocialCases.FindAsync(id);
        if (sc == null) return NotFound();

        sc.Status = "مفتوحة";
        sc.ClosedAt = null;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentUserId, "Staff", "SocialCases.ReOpen", "SocialCase", id);

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> ExportExcel(string search, string status, string priority, string caseType)
    {
        IQueryable<SocialCase> query = _db.SocialCases.Include(sc => sc.Student);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(sc => sc.Student.FullName.Contains(term) || sc.Student.StudentCode.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(sc => sc.Status == status);
        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(sc => sc.Priority == priority);
        if (!string.IsNullOrWhiteSpace(caseType))
            query = query.Where(sc => sc.CaseType == caseType);

        var data = await query.OrderByDescending(sc => sc.CreatedAt).ToListAsync();

        var rows = data.Select(sc => new[]
        {
            sc.Student.StudentCode ?? "",
            sc.Student.FullName,
            sc.CaseType ?? "",
            sc.Priority ?? "",
            sc.Status ?? "",
            sc.AssignedTo?.ToString() ?? "",
            sc.CreatedAt?.ToString("yyyy/MM/dd") ?? "",
            sc.ClosedAt?.ToString("yyyy/MM/dd") ?? "-"
        }).ToList();

        var cols = new[] { "الرقم الجامعي", "اسم الطالب", "نوع الحالة", "الأولوية", "الحالة", "المسؤول", "تاريخ الإنشاء", "تاريخ الإغلاق" };
        var title = "حالات البحث الاجتماعي";

        var excelBytes = _export.ExportToExcel(title, cols, rows, r => r);

        await _audit.LogAsync(CurrentUserId, "Staff", "SocialCases.ExportExcel", "SocialCase");
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SocialCases.xlsx");
    }

    public async Task<IActionResult> ExportPdf(string search, string status, string priority, string caseType)
    {
        IQueryable<SocialCase> query = _db.SocialCases.Include(sc => sc.Student);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(sc => sc.Student.FullName.Contains(term) || sc.Student.StudentCode.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(sc => sc.Status == status);
        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(sc => sc.Priority == priority);
        if (!string.IsNullOrWhiteSpace(caseType))
            query = query.Where(sc => sc.CaseType == caseType);

        var data = await query.OrderByDescending(sc => sc.CreatedAt).ToListAsync();

        var rows = data.Select(sc => new[]
        {
            sc.Student.StudentCode ?? "",
            sc.Student.FullName,
            sc.CaseType ?? "",
            sc.Priority ?? "",
            sc.Status ?? "",
            sc.AssignedTo?.ToString() ?? "-",
            sc.CreatedAt?.ToString("yyyy/MM/dd") ?? ""
        }).ToArray();

        var cols = new[] { "الرقم الجامعي", "الاسم", "نوع الحالة", "الأولوية", "الحالة", "المسؤول", "تاريخ الإنشاء" };

        await _audit.LogAsync(CurrentUserId, "Staff", "SocialCases.ExportPdf", "SocialCase");
        var pdfBytes = _export.ExportToPdf("حالات البحث الاجتماعي", cols, rows);
        return File(pdfBytes, "application/pdf", "SocialCases.pdf");
    }

    public async Task<IActionResult> Print(string search, string status, string priority, string caseType)
    {
        IQueryable<SocialCase> query = _db.SocialCases.Include(sc => sc.Student);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(sc => sc.Student.FullName.Contains(term) || sc.Student.StudentCode.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(sc => sc.Status == status);
        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(sc => sc.Priority == priority);
        if (!string.IsNullOrWhiteSpace(caseType))
            query = query.Where(sc => sc.CaseType == caseType);

        var data = await query.OrderByDescending(sc => sc.CreatedAt).ToListAsync();

        var vm = new SocialCasePrintVM
        {
            Title = "تقرير حالات البحث الاجتماعي",
            OrganizationName = "UniStay - إدارة المدن الجامعية",
            Cases = data.Select(sc => new SocialCasePrintItem
            {
                CaseId = sc.ID,
                StudentName = sc.Student.FullName,
                StudentCode = sc.Student.StudentCode,
                CaseType = sc.CaseType,
                Priority = sc.Priority,
                Status = sc.Status,
                AssignedTo = sc.AssignedTo,
                CreatedAt = sc.CreatedAt
            }),
            PrintedAt = DateTime.Now.ToString("yyyy/MM/dd HH:mm")
        };

        await _audit.LogAsync(CurrentUserId, "Staff", "SocialCases.Print", "SocialCase");
        return View(vm);
    }
}