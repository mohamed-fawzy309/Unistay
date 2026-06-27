using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Helpers;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.StudentProfiles;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
[RequirePermission("Students.View")]
public class StudentProfilesController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly IAuditService _audit;
    private readonly IReportExportService _export;
    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    public StudentProfilesController(AssuitDbContext db, IAuditService audit, IReportExportService export)
    {
        _db = db;
        _audit = audit;
        _export = export;
    }

    public async Task<IActionResult> Index(string search, string status, byte? year, string faculty, int page = 1)
    {
        const int pageSize = 20;

        IQueryable<Student> query = _db.Students
            .Include(s => s.Allocations);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.FullName.Contains(term) ||
                s.StudentCode.Contains(term) ||
                s.NationalID.Contains(term) ||
                s.Phone.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var isActive = status == "active";
            query = query.Where(s => s.IsActive == isActive);
        }

        if (year.HasValue)
            query = query.Where(s => s.AcademicYear == year.Value);

        if (!string.IsNullOrWhiteSpace(faculty))
            query = query.Where(s => s.Faculty != null && s.Faculty.Contains(faculty));

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        var students = await query
            .OrderByDescending(s => s.ID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentProfileItem
            {
                StudentId = s.ID,
                Name = s.FullName,
                StudentCode = s.StudentCode,
                NationalID = s.NationalID,
                Gender = s.Gender,
                IsActive = s.IsActive ?? false,
                Faculty = s.Faculty,
                AcademicYear = s.AcademicYear,
                Phone = s.Phone,
                HasActiveAllocation = s.Allocations.Any(a => a.Status == "Active")
            })
            .ToListAsync();

        var vm = new StudentProfileListVM
        {
            Students = students,
            SearchTerm = search,
            StatusFilter = status,
            AcademicYearFilter = year,
            FacultyFilter = faculty,
            Page = page,
            TotalPages = totalPages
        };

        await _audit.LogAsync(CurrentUserId, "Staff", "StudentProfiles.Index", "Student");
        return View(vm);
    }

    [AllowAnonymous]
    public async Task<IActionResult> SearchStudents(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return Json(new List<object>());

        var results = await _db.Students
            .Where(s => s.FullName.Contains(term) || s.StudentCode.Contains(term))
            .OrderBy(s => s.FullName)
            .Take(20)
            .Select(s => new { id = s.ID, name = s.FullName, studentCode = s.StudentCode })
            .ToListAsync();

        return Json(results);
    }

    public async Task<IActionResult> Details(int id)
    {
        var student = await _db.Students
            .Include(s => s.Guardians)
            .Include(s => s.Allocations).ThenInclude(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
            .FirstOrDefaultAsync(s => s.ID == id);

        if (student == null) return NotFound();

        var activeAlloc = student.Allocations.FirstOrDefault(a => a.Status == "Active");

        var vm = new StudentDetailsVM
        {
            BasicInfo = new StudentBasicInfo
            {
                StudentId = student.ID,
                Name = student.FullName,
                StudentCode = student.StudentCode,
                NationalID = student.NationalID,
                BirthDate = student.BirthDate,
                Gender = student.Gender,
                Photo = student.Photo,
                HasDisability = student.HasDisability ?? false,
                IsOrphan = student.IsOrphan ?? false,
                IsLowIncome = student.IsLowIncome ?? false,
                HasFamilyAbroad = student.HasFamilyAbroad ?? false,
                HasMedicalCondition = student.HasMedicalCondition ?? false,
                IsForeign = student.IsForeign ?? false,
                IsActive = student.IsActive ?? false
            },
            ContactInfo = new StudentContactInfo
            {
                Phone = student.Phone,
                Email = student.Email,
                Address = student.Address
            },
            AcademicInfo = new StudentAcademicInfo
            {
                Faculty = student.Faculty,
                AcademicYear = student.AcademicYear,
                Department = student.Department,
                GradePercentage = student.GradePercentage,
                GradeText = student.GradeText
            },
            HousingInfo = new StudentHousingInfo
            {
                IsAllocated = activeAlloc != null,
                BuildingName = activeAlloc?.CityRoom?.CityBuilding?.BuildingName,
                RoomNumber = activeAlloc?.CityRoom?.RoomNumber,
                BedNumber = activeAlloc?.BedNumber,
                AllocationStatus = activeAlloc?.Status
            },
            Guardians = student.Guardians.Select(g => new GuardianInfo
            {
                GuardianId = g.ID,
                GuardianType = g.GuardianType,
                FullName = g.FullName,
                NationalID = g.NationalID,
                Phone = g.Phone,
                Job = g.Job,
                Address = g.Address,
                IsDeceased = g.IsDeceased ?? false
            })
        };

        await _audit.LogAsync(CurrentUserId, "Staff", "StudentProfiles.Details", "Student", student.ID);
        return View(vm);
    }

    [RequirePermission("StudentStatus.View")]
    public async Task<IActionResult> Status(int id)
    {
        var student = await _db.Students
            .Include(s => s.Allocations).ThenInclude(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
            .Include(s => s.Payments)
            .Include(s => s.Violations)
            .Include(s => s.Absences)
            .Include(s => s.MealConsumptions).ThenInclude(mc => mc.Meal)
            .FirstOrDefaultAsync(s => s.ID == id);

        if (student == null) return NotFound();

        var vm = new StudentStatusVM
        {
            StudentId = student.ID,
            StudentName = student.FullName,
            StudentCode = student.StudentCode,
            IsActive = student.IsActive ?? false,

            // البيانات الأساسية
            NationalID = student.NationalID,
            Gender = student.Gender,
            BirthDate = student.BirthDate,
            Religion = student.Religion,
            Email = student.Email,
            Phone = student.Phone,
            Faculty = student.Faculty,
            Department = student.Department,
            AcademicYear = student.AcademicYear,
            GradeText = student.GradeText,
            Governorate = student.Governorate,
            Markaz = student.Markaz,
            City = student.City,
            Address = student.Address,
            DistanceFromUniv = student.DistanceFromUniv,

            Allocations = student.Allocations.Select(a => new AllocationInfo
            {
                AllocationId = a.ID,
                BuildingName = a.CityRoom?.CityBuilding?.BuildingName,
                RoomNumber = a.CityRoom?.RoomNumber,
                BedNumber = a.BedNumber,
                Status = a.Status,
                AcademicYear = a.AcademicYear,
                StartDate = a.StartDate,
                EndDate = a.EndDate
            }),

            Payments = student.Payments.Select(p => new PaymentInfo
            {
                PaymentId = p.ID,
                Amount = p.Amount,
                PaidAmount = p.PaidAmount,
                Status = p.Status,
                PaymentDate = p.RecordedAt,
                PaymentType = p.PaymentType
            }),

            Violations = student.Violations.Select(v => new ViolationInfo
            {
                ViolationId = v.ID,
                ViolationType = v.ViolationType,
                Description = v.Description,
                ViolationDate = v.RecordedAt,
                Status = v.Status,
                Severity = v.Severity
            }),

            Absences = student.Absences.Select(a => new AbsenceInfo
            {
                AbsenceId = a.ID,
                AbsenceDate = a.AbsenceDate,
                ToDate = a.ToDate,
                AbsenceType = a.AbsenceType,
                Status = a.Status,
                Reason = a.Reason
            }),

            Meals = student.MealConsumptions.Select(mc => new MealInfo
            {
                ConsumptionId = mc.ID,
                MealType = mc.Meal.MealType,
                MealDate = mc.MealDate
            }),

            TotalAllocations = student.Allocations.Count,
            TotalPayments = student.Payments.Count,
            TotalPaid = student.Payments.Where(p => p.Status == "Completed").Sum(p => p.PaidAmount),
            TotalViolations = student.Violations.Count,
            TotalAbsences = student.Absences.Count,
            TotalMeals = student.MealConsumptions.Count
        };

        await _audit.LogAsync(CurrentUserId, "Staff", "StudentProfiles.Status", "Student", student.ID);
        return View(vm);
    }

    public async Task<IActionResult> ExportExcel(string search, string status, byte? year, string faculty)
    {
        IQueryable<Student> query = _db.Students;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.FullName.Contains(term) || s.StudentCode.Contains(term) || s.NationalID.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            var isActive = status == "active";
            query = query.Where(s => s.IsActive == isActive);
        }
        if (year.HasValue)
            query = query.Where(s => s.AcademicYear == year.Value);
        if (!string.IsNullOrWhiteSpace(faculty))
            query = query.Where(s => s.Faculty != null && s.Faculty.Contains(faculty));

        var data = await query.OrderByDescending(s => s.ID).ToListAsync();

        var rows = data.Select(s => new[]
        {
            s.StudentCode ?? "",
            s.FullName,
            s.NationalID,
            s.Gender == "Male" ? "ذكر" : s.Gender == "Female" ? "أنثى" : s.Gender,
            s.Faculty ?? "",
            s.AcademicYear?.ToString() ?? "",
            s.IsActive == true ? "نشط" : "غير نشط",
            s.Phone,
            s.Email
        }).ToList();

        var cols = new[] { "الرقم الجامعي", "الاسم", "الرقم القومي", "النوع", "الكلية", "السنة الدراسية", "الحالة", "الهاتف", "البريد" };
        var title = "قاعدة بيانات الطلاب";

        var excelBytes = _export.ExportToExcel(title, cols, rows, r => r);

        await _audit.LogAsync(CurrentUserId, "Staff", "StudentProfiles.ExportExcel", "Student");
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Students.xlsx");
    }

    public async Task<IActionResult> ExportPdf(string search, string status, byte? year, string faculty)
    {
        IQueryable<Student> query = _db.Students;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.FullName.Contains(term) || s.StudentCode.Contains(term) || s.NationalID.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            var isActive = status == "active";
            query = query.Where(s => s.IsActive == isActive);
        }
        if (year.HasValue)
            query = query.Where(s => s.AcademicYear == year.Value);
        if (!string.IsNullOrWhiteSpace(faculty))
            query = query.Where(s => s.Faculty != null && s.Faculty.Contains(faculty));

        var data = await query.OrderByDescending(s => s.ID).ToListAsync();

        var rows = data.Select(s => new[]
        {
            s.StudentCode ?? "",
            s.FullName,
            s.Faculty ?? "",
            s.AcademicYear?.ToString() ?? "",
            s.IsActive == true ? "نشط" : "غير نشط",
            s.Phone
        }).ToArray();

        var cols = new[] { "الرقم الجامعي", "الاسم", "الكلية", "السنة", "الحالة", "الهاتف" };

        await _audit.LogAsync(CurrentUserId, "Staff", "StudentProfiles.ExportPdf", "Student");
        var pdfBytes = _export.ExportToPdf("قاعدة بيانات الطلاب", cols, rows);
        return File(pdfBytes, "application/pdf", "Students.pdf");
    }

    public async Task<IActionResult> Print(int id)
    {
        var student = await _db.Students
            .Include(s => s.Guardians)
            .Include(s => s.Allocations).ThenInclude(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
            .FirstOrDefaultAsync(s => s.ID == id);

        if (student == null) return NotFound();

        var activeAlloc = student.Allocations.FirstOrDefault(a => a.Status == "Active");

        var vm = new StudentProfilePrintVM
        {
            Title = "بطاقة بيانات الطالب",
            BasicInfo = new StudentBasicInfo
            {
                StudentId = student.ID,
                Name = student.FullName,
                StudentCode = student.StudentCode,
                NationalID = student.NationalID,
                BirthDate = student.BirthDate,
                Gender = student.Gender,
                Photo = student.Photo,
                HasDisability = student.HasDisability ?? false,
                IsOrphan = student.IsOrphan ?? false,
                IsLowIncome = student.IsLowIncome ?? false,
                HasFamilyAbroad = student.HasFamilyAbroad ?? false,
                HasMedicalCondition = student.HasMedicalCondition ?? false,
                IsForeign = student.IsForeign ?? false,
                IsActive = student.IsActive ?? false
            },
            ContactInfo = new StudentContactInfo
            {
                Phone = student.Phone,
                Email = student.Email,
                Address = student.Address
            },
            AcademicInfo = new StudentAcademicInfo
            {
                Faculty = student.Faculty,
                AcademicYear = student.AcademicYear,
                Department = student.Department,
                GradePercentage = student.GradePercentage,
                GradeText = student.GradeText
            },
            HousingInfo = new StudentHousingInfo
            {
                IsAllocated = activeAlloc != null,
                BuildingName = activeAlloc?.CityRoom?.CityBuilding?.BuildingName,
                RoomNumber = activeAlloc?.CityRoom?.RoomNumber,
                BedNumber = activeAlloc?.BedNumber,
                AllocationStatus = activeAlloc?.Status
            },
            Guardians = student.Guardians.Select(g => new GuardianInfo
            {
                GuardianId = g.ID,
                GuardianType = g.GuardianType,
                FullName = g.FullName,
                NationalID = g.NationalID,
                Phone = g.Phone,
                Job = g.Job,
                Address = g.Address,
                IsDeceased = g.IsDeceased ?? false
            }),
            PrintedAt = DateTime.Now.ToString("yyyy/MM/dd HH:mm")
        };

        await _audit.LogAsync(CurrentUserId, "Staff", "StudentProfiles.Print", "Student", student.ID);
        return View(vm);
    }
}