using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Admin;
using UniStay.ViewModels.Permissions;

namespace UniStay.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IPermissionService _perm;
        private readonly IUniversityApiService _api;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;
        private readonly IPasswordService _passwordService;

        public AdminController(
            AssuitDbContext db,
            IPermissionService perm,
            IUniversityApiService api,
            IAuditService audit,
            IEmailService email,
            IPasswordService passwordService)
        {
            _db = db;
            _perm = perm;
            _api = api;
            _audit = audit;
            _email = email;
            _passwordService = passwordService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        private string GetCurrentAcademicYear()
        {
            var year = DateTime.Now.Year;
            return DateTime.Now.Month >= 9 ? $"{year}-{year + 1}" : $"{year - 1}-{year}";
        }

        [RequirePermission("Dashboard.View", "CanView")]
        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var currentYear = GetCurrentAcademicYear();

            var vm = new DashboardViewModel
            {
                PendingCount = await _db.Applications.CountAsync(a => a.Status == "Pending" && a.AcademicYear == currentYear),
                AcceptedCount = await _db.Applications.CountAsync(a => a.Status == "Accepted" && a.AcademicYear == currentYear),
                RejectedCount = await _db.Applications.CountAsync(a => a.Status == "Rejected" && a.AcademicYear == currentYear),
                TotalStudents = await _db.Students.CountAsync(s => s.IsDeleted != true),
                AllocatedCount = await _db.Allocations.CountAsync(a => a.Status == "Active"),
                CityCount = await _db.DormitoryCities.CountAsync(c => c.IsActive && !c.IsDeleted),
                BuildingCount = await _db.CityBuildings.CountAsync(b => b.IsActive != false && b.IsDeleted != true),
                RoomCount = await _db.CityRooms.CountAsync(r => r.IsActive != false && r.IsDeleted != true),
                UserCount = await _db.SystemUsers.CountAsync(u => !u.IsDeleted),
                AdminCount = await _db.SystemUsers.CountAsync(u => u.IsSuperAdmin && !u.IsDeleted),
                RoleCount = await _db.Set<UniStay.Models.Role>().CountAsync(r => r.IsActive),
                TodayApplications = await _db.Applications.CountAsync(a => a.CreatedAt!.Value.Date == now.Date),
                TotalApplications = await _db.Applications.CountAsync()
            };

            vm.LatestApplications = await _db.Applications
                .Include(a => a.Student)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new ApplicationRowViewModel
                {
                    ID = a.ID,
                    StudentName = a.Student!.FullName,
                    NationalID = a.Student.NationalID,
                    Faculty = a.Student.Faculty,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt!.Value
                })
                .ToListAsync();

            var recentLogs = await _db.AuditLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToListAsync();
            var userIds = recentLogs.Where(l => l.UserType == "System").Select(l => l.UserID).Distinct().ToList();
            var userNames = await _db.SystemUsers.Where(u => userIds.Contains(u.ID)).ToDictionaryAsync(u => u.ID, u => u.Name);
            vm.RecentAuditLogs = recentLogs.Select(l => new AuditLogRowViewModel
            {
                ID = l.ID,
                UserDisplayName = l.UserType == "System" && userNames.ContainsKey(l.UserID) ? userNames[l.UserID] : l.UserID.ToString(),
                UserType = l.UserType,
                Action = l.Action,
                ActionDisplay = l.Action,
                TableName = l.TableName,
                CreatedAt = l.CreatedAt
            }).ToList();

            return View(vm);
        }



       // ──────────────────────────────────────────────────────────────────────────────
       // 1. إدارة الطلبات
       // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("PendingRegistrations.Manage", "CanView")]
        public async Task<IActionResult> PendingApplications(
    string? status = null,
    string? studentType = null,
    int? cityId = null,
    string? faculty = null,
    string? search = null,
    DateOnly? from = null,
    DateOnly? to = null,
    int page = 1)
        {
            var query = _db.Applications
                .Include(a => a.Student)
                .Include(a => a.DormitoryCity)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(a => a.Status == status);

            if (!string.IsNullOrEmpty(studentType))
                query = query.Where(a => a.StudentType == studentType);

            if (cityId.HasValue)
                query = query.Where(a => a.DormitoryCityID == cityId.Value);

            if (!string.IsNullOrEmpty(faculty))
                query = query.Where(a => a.Student!.Faculty == faculty);

            if (from.HasValue)
                query = query.Where(a => a.CreatedAt >= from.Value.ToDateTime(TimeOnly.MinValue));

            if (to.HasValue)
                query = query.Where(a => a.CreatedAt <= to.Value.ToDateTime(TimeOnly.MaxValue));

            var total = await query.CountAsync();

            var apps = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * 20)
                .Take(20)
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
                    CreatedAt = a.CreatedAt!.Value,
                    ServerVerificationStatus = a.ServerVerificationStatus
                })
                .ToListAsync();

            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);
            ViewBag.FilterStatus = status;
            ViewBag.FilterStudentType = studentType;
            ViewBag.FilterCityId = cityId;
            ViewBag.FilterFaculty = faculty;
            ViewBag.FilterFrom = from;
            ViewBag.FilterTo = to;
            ViewBag.Search = search;

            return View(apps);
        }

        [HttpGet]
        [RequirePermission("PendingRegistrations.Manage", "CanEdit")]
        public async Task<IActionResult> ReviewApplication(int id)
        {
            var app = await _db.Applications
                .Include(a => a.Student)
                    .ThenInclude(s => s!.Guardians)
                .Include(a => a.DormitoryCity)
                .Include(a => a.ReviewedByNavigation)
                .Include(a => a.ServerVerificationByNavigation)
                .Include(a => a.Allocation)
                    .ThenInclude(al => al!.CityRoom)
                        .ThenInclude(r => r.CityBuilding)
                .FirstOrDefaultAsync(a => a.ID == id);

            if (app == null) return NotFound();

            return View(app);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("PendingRegistrations.Manage", "CanEdit")]
        public async Task<IActionResult> ReviewApplication(int id, ReviewDecisionViewModel model)
        {
            var app = await _db.Applications
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.ID == id);

            if (app == null) return NotFound();

            var oldStatus = app.Status;

            if (model.Decision == "Rejected" && string.IsNullOrWhiteSpace(model.RejectionReason))
            {
                ModelState.AddModelError("RejectionReason", "سبب الرفض إلزامي");
                return RedirectToAction("ReviewApplication", new { id });
            }

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

            // إشعار الطالب بالإيميل
            if (app.Student != null && !string.IsNullOrEmpty(app.Student.Email))
            {
                string subject = model.Decision == "Accepted"
                    ? "تهانينا! تم قبول طلبك - UniStay"
                    : "نتيجة مراجعة طلب السكن - UniStay";

                string body = model.Decision == "Accepted"
                    ? $"<h3>تهانينا!</h3><p>عزيزي {app.Student.FullName}، تم قبول طلب السكن الخاص بك.</p>"
                    : $"<h3>نأسف</h3><p>عزيزي {app.Student.FullName}، تم رفض طلب السكن الخاص بك.</p>"
                    + (model.Decision == "Rejected" ? $"<p>السبب: {model.RejectionReason}</p>" : "");

                var emailType = model.Decision == "Accepted" ? EmailType.ApplicationAccepted : EmailType.ApplicationRejected;
                await _email.SendAsync(app.Student.Email, subject, body, emailType, app.Student.ID);
            }

            TempData["Success"] = model.Decision == "Accepted" ? "تم قبول الطلب بنجاح" : "تم رفض الطلب";
            return RedirectToAction("PendingApplications");
        }

        [HttpPost]
        [RequirePermission("PendingRegistrations.Manage", "CanEdit")]
        public async Task<IActionResult> VerifyFromServer(int id)
        {
            var app = await _db.Applications
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.ID == id);

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
                null, new { result.Found, result.IsMatch, Differences = result.Differences });

            return Json(new
            {
                success = true,
                status = app.ServerVerificationStatus,
                apiData = result,
                localData = new
                {
                    app.Student.FullName,
                    app.Student.Faculty,
                    app.Student.AcademicYear,
                    app.Student.GradePercentage,
                    app.Student.IsEnrolled
                },
                comparison = result.Differences
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("PendingRegistrations.Manage", "CanEdit")]
        public async Task<IActionResult> QuickApprove(int id)
        {
            var app = await _db.Applications.FindAsync(id);
            if (app == null) return NotFound();

            var oldStatus = app.Status;
            app.Status = "Accepted";
            app.ReviewedBy = CurrentUserId;
            app.ReviewedAt = DateTime.UtcNow;
            app.LastUpdatedAt = DateTime.UtcNow;
            app.LastUpdatedBy = CurrentUserId;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff",
                "Application.QuickApprove", "Application", id,
                new { Status = oldStatus }, new { Status = "Accepted" });

            TempData["Success"] = "تم قبول الطلب سريعاً";
            return RedirectToAction("PendingApplications");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("PendingRegistrations.Manage", "CanEdit")]
        public async Task<IActionResult> QuickReject(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "سبب الرفض إلزامي";
                return RedirectToAction("PendingApplications");
            }

            var app = await _db.Applications.FindAsync(id);
            if (app == null) return NotFound();

            var oldStatus = app.Status;
            app.Status = "Rejected";
            app.RejectionReason = reason;
            app.ReviewedBy = CurrentUserId;
            app.ReviewedAt = DateTime.UtcNow;
            app.LastUpdatedAt = DateTime.UtcNow;
            app.LastUpdatedBy = CurrentUserId;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff",
                "Application.QuickReject", "Application", id,
                new { Status = oldStatus }, new { Status = "Rejected", Reason = reason });

            TempData["Success"] = "تم رفض الطلب";
            return RedirectToAction("PendingApplications");
        }

        [HttpGet]
        [RequirePermission("Coordination.Manage", "CanView")]
        public async Task<IActionResult> AllApplications(
    string? status = null,
    string? studentType = null,
    int? cityId = null,
    string? faculty = null,
    string? search = null,
    DateOnly? from = null,
    DateOnly? to = null,
    int page = 1)
        {
            var query = _db.Applications
                .Include(a => a.Student)
                .Include(a => a.DormitoryCity)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(a => a.Status == status);

            if (!string.IsNullOrEmpty(studentType))
                query = query.Where(a => a.StudentType == studentType);

            if (cityId.HasValue)
                query = query.Where(a => a.DormitoryCityID == cityId.Value);

            if (!string.IsNullOrEmpty(faculty))
                query = query.Where(a => a.Student!.Faculty == faculty);
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a =>
                    a.Student!.FullName.Contains(search) ||
                    a.Student.NationalID.Contains(search));
            }


            if (from.HasValue)
                query = query.Where(a => a.CreatedAt >= from.Value.ToDateTime(TimeOnly.MinValue));

            if (to.HasValue)
                query = query.Where(a => a.CreatedAt <= to.Value.ToDateTime(TimeOnly.MaxValue));

            var total = await query.CountAsync();

            var apps = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * 30)
                .Take(30)
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
                    CreatedAt = a.CreatedAt!.Value,
                    ReviewedAt = a.ReviewedAt,
                    ReviewedByName = a.ReviewedByNavigation!.Name,
                    ServerVerificationStatus = a.ServerVerificationStatus
                })
                .ToListAsync();

            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / 30.0);
            ViewBag.TotalCount = total;

            return View(apps);
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 2. إدارة الطلاب
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("Students.Manage", "CanView")]
        public async Task<IActionResult> Students(
            string? search = null,
            string? faculty = null,
            string? gender = null,
            int? cityId = null,
            int? buildingId = null,
            string? housingStatus = null,
            byte? academicYear = null,
            bool? isActive = null,
            int page = 1)
        {
            var query = _db.Students
                .Where(s => s.IsDeleted != true)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s =>
                    s.FullName.Contains(search) ||
                    s.NationalID.Contains(search) ||
                    (s.StudentCode != null && s.StudentCode.Contains(search)));

            if (!string.IsNullOrEmpty(faculty))
                query = query.Where(s => s.Faculty == faculty);


            if (!string.IsNullOrEmpty(gender))
                query = query.Where(s => s.Gender == gender);

            if (academicYear.HasValue)
                query = query.Where(s => s.AcademicYear == academicYear.Value);

            if (isActive.HasValue)
                query = query.Where(s => s.IsActive == isActive.Value);

            if (cityId.HasValue)
                query = query.Where(s => s.Allocations.Any(a => a.CityRoom.CityBuilding.DormitoryCityID == cityId.Value));

            if (buildingId.HasValue)
                query = query.Where(s => s.Allocations.Any(a => a.CityRoom.CityBuildingID == buildingId.Value));

            if (!string.IsNullOrEmpty(housingStatus))
                query = query.Where(s => s.Allocations.Any(a => a.Status == housingStatus));

            var total = await query.CountAsync();

            var students = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * 20)
                .Take(20)
                .Select(s => new StudentRowViewModel
                {
                    ID = s.ID,
                    FullName = s.FullName,
                    NationalID = s.NationalID,
                    Gender = s.Gender,
                    Faculty = s.Faculty,
                    AcademicYear = s.AcademicYear,
                    Phone = s.Phone,
                    Email = s.Email,
                    IsActive = s.IsActive == true,
                    CreatedAt = s.CreatedAt!.Value,
                    LatestApplicationStatus = _db.Applications
                        .Where(a => a.StudentID == s.ID)
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.Status)
                        .FirstOrDefault()!,
                    CityName = s.Allocations
                        .Where(a => a.Status == "Active")
                        .Select(a => a.CityRoom.CityBuilding.DormitoryCity.Name)
                        .FirstOrDefault(),
                    BuildingName = s.Allocations
                        .Where(a => a.Status == "Active")
                        .Select(a => a.CityRoom.CityBuilding.BuildingName)
                        .FirstOrDefault(),
                    RoomNumber = s.Allocations
                        .Where(a => a.Status == "Active")
                        .Select(a => a.CityRoom.RoomNumber)
                        .FirstOrDefault(),
                    BedNumber = s.Allocations
                        .Where(a => a.Status == "Active")
                        .Select(a => (byte?)a.BedNumber)
                        .FirstOrDefault(),
                    HousingStatus = s.Allocations
                        .OrderByDescending(a => a.AllocatedAt)
                        .Select(a => a.Status)
                        .FirstOrDefault()
                })
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);

            ViewBag.Cities = await _db.DormitoryCities.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.Buildings = await _db.CityBuildings.Where(b => b.IsActive).OrderBy(b => b.BuildingName).ToListAsync();

            return View(students);
        }

        [HttpGet]
        [RequirePermission("Students.Manage", "CanView")]
        public async Task<IActionResult> StudentDetails(int id)
        {
            var student = await _db.Students
                .Include(s => s.Guardians)
                .Include(s => s.Applications)
                    .ThenInclude(a => a.DormitoryCity)
                .Include(s => s.Allocations)
                    .ThenInclude(al => al.CityRoom)
                        .ThenInclude(r => r.CityBuilding)
                .Include(s => s.Absences)
                .Include(s => s.Violations)
                .Include(s => s.Payments)
                .Include(s => s.StudentLogin)
                .FirstOrDefaultAsync(s => s.ID == id);

            if (student == null) return NotFound();

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Students.Manage", "CanEdit")]
        public async Task<IActionResult> EditStudent(int id, EditStudentViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("StudentDetails", new { id });

            var student = await _db.Students.FindAsync(id);
            if (student == null) return NotFound();

            var oldValues = new
            {
                student.FullName,
                student.Faculty,
                student.AcademicYear,
                student.Phone,
                student.Email,
                student.Address,
                student.GradePercentage
            };

            student.FullName = model.FullName;
            student.Faculty = model.Faculty;
            student.AcademicYear = (byte?)model.AcademicYear;
            student.Phone = model.Phone;
            student.Email = model.Email;
            student.Address = model.Address;
            student.GradePercentage = model.GradePercentage;
            student.Governorate = model.Governorate;
            student.City = model.City;
            student.Markaz = model.Markaz;
            student.DistanceFromUniv = model.DistanceFromUniv;
            student.HasMedicalCondition = model.HasMedicalCondition;
            student.MedicalDescription = model.MedicalDescription;
            student.LastUpdatedAt = DateTime.UtcNow;
            student.LastUpdatedBy = CurrentUserId;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Student.Edit",
                "Student", id, oldValues, new
                {
                    model.FullName, model.Faculty, model.AcademicYear,
                    model.Phone, model.Email
                });

            TempData["Success"] = "تم تحديث بيانات الطالب بنجاح";
            return RedirectToAction("StudentDetails", new { id });
        }

        [HttpGet]
        [RequirePermission("Students.Manage", "CanView")]
        public async Task<IActionResult> StudentStatement(string? search = null, string? nationalId = null, int? id = null)
        {
            if (!id.HasValue && !string.IsNullOrEmpty(nationalId))
            {
                var found = await _db.Students.Where(s => s.NationalID == nationalId && s.IsDeleted != true).FirstOrDefaultAsync();
                if (found != null) id = found.ID;
            }

            if (!id.HasValue && !string.IsNullOrEmpty(search))
            {
                var found = await _db.Students.Where(s => s.FullName.Contains(search) && s.IsDeleted != true).FirstOrDefaultAsync();
                if (found != null) id = found.ID;
            }

            if (id.HasValue)
            {
                var student = await _db.Students
                    .Include(s => s.Allocations).ThenInclude(a => a.CityRoom).ThenInclude(r => r.CityBuilding).ThenInclude(b => b.DormitoryCity)
                    .Include(s => s.Payments)
                    .Include(s => s.Absences)
                    .Include(s => s.Violations)
                    .Include(s => s.Applications)
                    .FirstOrDefaultAsync(s => s.ID == id);

                if (student == null) return NotFound();

                var vm = new StudentStatementViewModel
                {
                    BasicInfo = new StudentBasicInfo
                    {
                        ID = student.ID,
                        FullName = student.FullName,
                        NationalID = student.NationalID,
                        Gender = student.Gender,
                        Faculty = student.Faculty,
                        AcademicYear = student.AcademicYear,
                        Phone = student.Phone,
                        Email = student.Email,
                        Governorate = student.Governorate,
                        Markaz = student.Markaz,
                        City = student.City,
                        GradePercentage = student.GradePercentage,
                        IsActive = student.IsActive == true
                    },
                    CurrentHousing = student.Allocations
                        .Where(a => a.Status == "Active")
                        .Select(a => new StudentHousingInfo
                        {
                            CityName = a.CityRoom.CityBuilding.DormitoryCity.Name,
                            BuildingName = a.CityRoom.CityBuilding.BuildingName,
                            RoomNumber = a.CityRoom.RoomNumber,
                            BedNumber = a.BedNumber,
                            StartDate = a.StartDate,
                            EndDate = a.EndDate,
                            Status = a.Status
                        })
                        .FirstOrDefault(),
                    Payments = student.Payments
                        .OrderByDescending(p => p.RecordedAt)
                        .Select(p => new PaymentRow
                        {
                            ID = p.ID,
                            PaymentType = p.PaymentType,
                            Amount = p.Amount,
                            PaidAmount = p.PaidAmount,
                            Status = p.Status,
                            RecordedAt = p.RecordedAt
                        })
                        .ToList(),
                    Absences = student.Absences
                        .OrderByDescending(a => a.AbsenceDate)
                        .Select(a => new AbsenceRow
                        {
                            ID = a.ID,
                            AbsenceDate = a.AbsenceDate,
                            AbsenceType = a.AbsenceType,
                            Status = a.Status,
                            Reason = a.Reason
                        })
                        .ToList(),
                    Violations = student.Violations
                        .OrderByDescending(v => v.RecordedAt)
                        .Select(v => new ViolationRow
                        {
                            ID = v.ID,
                            ViolationType = v.ViolationType,
                            Description = v.Description,
                            Severity = v.Severity,
                            FineAmount = v.FineAmount,
                            Status = v.Status
                        })
                        .ToList(),
                    Applications = student.Applications
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => new ApplicationRow
                        {
                            ID = a.ID,
                            AcademicYear = a.AcademicYear ?? "",
                            Status = a.Status,
                            CreatedAt = a.CreatedAt
                        })
                        .ToList()
                };

                return View(vm);
            }

            ViewBag.StudentsList = await _db.Students
                .Where(s => s.IsDeleted != true)
                .OrderBy(s => s.FullName)
                .Select(s => new { s.ID, s.FullName, s.NationalID })
                .ToListAsync();

            return View();
        }

        [HttpGet]
        [RequirePermission("Students.Manage", "CanView")]
        public async Task<IActionResult> SocialCases(
            string? caseType = null,
            string? status = null,
            string? priority = null,
            string? search = null,
            int page = 1)
        {
            var query = _db.SocialCases
                .Include(sc => sc.Student)
                .Include(sc => sc.AssignedToNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(caseType))
                query = query.Where(sc => sc.CaseType == caseType);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(sc => sc.Status == status);

            if (!string.IsNullOrEmpty(priority))
                query = query.Where(sc => sc.Priority == priority);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(sc =>
                    sc.Student.FullName.Contains(search) ||
                    sc.Student.NationalID.Contains(search));

            var total = await query.CountAsync();
            var openCount = await query.CountAsync(sc => sc.Status == "Open");
            var resolvedCount = await query.CountAsync(sc => sc.Status == "Resolved");
            var highCount = await query.CountAsync(sc => sc.Priority == "High");

            var cases = await query
                .OrderByDescending(sc => sc.CreatedAt)
                .Skip((page - 1) * 30)
                .Take(30)
                .Select(sc => new AdminSocialCaseRow
                {
                    ID = sc.ID,
                    StudentID = sc.StudentID,
                    StudentName = sc.Student.FullName,
                    NationalID = sc.Student.NationalID,
                    Faculty = sc.Student.Faculty,
                    CaseType = sc.CaseType,
                    Description = sc.Description,
                    Status = sc.Status,
                    Priority = sc.Priority,
                    AssignedTo = sc.AssignedToNavigation != null ? sc.AssignedToNavigation.Name : "",
                    CreatedAt = sc.CreatedAt,
                })
                .ToListAsync();

            var vm = new AdminSocialCaseViewModel
            {
                Cases = cases,
                TotalCases = total,
                OpenCases = openCount,
                ResolvedCases = resolvedCount,
                HighPriority = highCount,
                Page = page,
                TotalPages = (int)Math.Ceiling(total / 30.0),
                Search = search,
                CaseType = caseType,
                Status = status,
                Priority = priority
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Students.Manage", "CanEdit")]
        public async Task<IActionResult> UpdateSocialCaseStatus(int id, string status, string? notes = null)
        {
            var socialCase = await _db.SocialCases.FindAsync(id);
            if (socialCase == null) return Json(new { success = false, message = "الحالة غير موجودة" });

            var oldStatus = socialCase.Status;
            socialCase.Status = status;
            if (status == "Resolved" || status == "Closed")
                socialCase.ClosedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "SocialCase.StatusUpdate",
                "SocialCase", id, new { Status = oldStatus }, new { Status = status });

            return Json(new { success = true, message = "تم تحديث الحالة بنجاح" });
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 3. المدن والمباني
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("DormitoryCities.Manage", "CanView")]
        public async Task<IActionResult> Cities()
        {
            var cities = await _db.DormitoryCities
                .Include(c => c.University)
                .Include(c => c.CityBuildings)
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Universities = await _db.Universities
                .OrderBy(u => u.Name)
                .ToListAsync();

            return View(cities);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("DormitoryCities.Manage", "CanCreate")]
        public async Task<IActionResult> Cities(CreateCityViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("Cities");

            var city = new DormitoryCity
            {
                UniversityID = model.UniversityID,
                Name = model.Name,
                CityType = model.CityType,
                Location = model.Location,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };

            _db.DormitoryCities.Add(city);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "City.Create",
                "DormitoryCity", city.ID, null, new { city.Name, city.CityType });

            TempData["Success"] = "تم إنشاء المدينة الجامعية";
            return RedirectToAction("Cities");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("DormitoryCities.Manage", "CanEdit")]
        public async Task<IActionResult> EditCity(EditCityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("Cities");
            }

            var city = await _db.DormitoryCities.FindAsync(model.ID);
            if (city == null) return NotFound();

            var old = new { city.Name, city.CityType, city.IsActive };

            city.Name = model.Name;
            city.CityType = model.CityType;
            city.Location = model.Location;
            city.IsActive = model.IsActive;
            city.LastUpdatedAt = DateTime.UtcNow;
            city.LastUpdatedBy = CurrentUserId;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "City.Edit",
                "DormitoryCity", model.ID, old, new { model.Name, model.CityType, model.IsActive });

            TempData["Success"] = "تم تحديث المدينة";
            return RedirectToAction("Cities");
        }

        [HttpGet]
        [RequirePermission("DormitoryCities.Manage", "CanEdit")]
        public async Task<IActionResult> CityConfig(int id)
        {
            var city = await _db.DormitoryCities
                .FirstOrDefaultAsync(c => c.ID == id && !c.IsDeleted);

            if (city == null) return NotFound();

            var config = await _db.CityConfigurations
                .FirstOrDefaultAsync(c => c.DormitoryCityID == id);

            if (config == null)
            {
                config = new CityConfiguration { DormitoryCityID = id };
                _db.CityConfigurations.Add(config);
                await _db.SaveChangesAsync();
            }

            ViewBag.CityName = city.Name;
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("DormitoryCities.Manage", "CanEdit")]
        public async Task<IActionResult> CityConfig(CityConfiguration model)
        {
            var config = await _db.CityConfigurations
                .FirstOrDefaultAsync(c => c.ID == model.ID);

            if (config == null) return NotFound();

            var old = new
            {
                config.StandardFee, config.PremiumFee, config.VIPFee,
                config.SecurityDeposit, config.MealFee,
                config.MinDistanceKm, config.MinGradePercentage, config.MaxAge,
                config.AutoCoordinationEnabled
            };

            config.StandardFee = model.StandardFee;
            config.PremiumFee = model.PremiumFee;
            config.VIPFee = model.VIPFee;
            config.ForeignStudentFee = model.ForeignStudentFee;
            config.SecurityDeposit = model.SecurityDeposit;
            config.MealFee = model.MealFee;
            config.RamadanMealFee = model.RamadanMealFee;
            config.ChristianMealFee = model.ChristianMealFee;
            config.MinDistanceKm = model.MinDistanceKm;
            config.MinGradePercentage = model.MinGradePercentage;
            config.MaxAge = model.MaxAge;
            config.AutoCoordinationEnabled = model.AutoCoordinationEnabled;
            config.MaxBedsPerRoom = model.MaxBedsPerRoom;
            config.AllowStudentBedSelection = model.AllowStudentBedSelection;
            config.ExcludedFaculties = model.ExcludedFaculties;
            config.AllowedFacultiesOnly = model.AllowedFacultiesOnly;
            config.LastUpdatedAt = DateTime.UtcNow;
            config.LastUpdatedBy = CurrentUserId;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "CityConfig.Update",
                "CityConfiguration", model.ID, old, new
                {
                    model.StandardFee, model.PremiumFee, model.VIPFee,
                    model.AutoCoordinationEnabled
                });

            TempData["Success"] = "تم تحديث إعدادات المدينة";
            return RedirectToAction("CityConfig", new { id = config.DormitoryCityID });
        }

        [HttpGet]
        [RequirePermission("Buildings.Manage", "CanView")]
        public async Task<IActionResult> Buildings(int? cityId = null)
        {
            var query = _db.CityBuildings
                .Include(b => b.DormitoryCity)
                .Where(b => !b.IsDeleted)
                .AsQueryable();

            if (cityId.HasValue)
                query = query.Where(b => b.DormitoryCityID == cityId.Value);

            var buildings = await query
                .OrderBy(b => b.DormitoryCity.Name)
                .ThenBy(b => b.BuildingName)
                .Select(b => new BuildingRowViewModel
                {
                    ID = b.ID,
                    BuildingName = b.BuildingName,
                    BuildingType = b.BuildingType,
                    FloorCount = b.FloorCount,
                    CityName = b.DormitoryCity.Name,
                    CityID = b.DormitoryCityID,
                    RoomCount = b.CityRooms.Count,
                    IsActive = b.IsActive
                })
                .ToListAsync();

            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .ToListAsync();

            ViewBag.FilterCityId = cityId;

            return View(buildings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Buildings.Manage", "CanCreate")]
        public async Task<IActionResult> Buildings(CreateBuildingViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("Buildings");

            var building = new CityBuilding
            {
                DormitoryCityID = model.DormitoryCityID,
                BuildingName = model.BuildingName,
                BuildingType = model.BuildingType,
                FloorCount = (byte)model.FloorCount,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };

            _db.CityBuildings.Add(building);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Building.Create",
                "CityBuilding", building.ID, null,
                new { Name = building.BuildingName, Type = building.BuildingType, CityId = building.DormitoryCityID });

            TempData["Success"] = "تم إنشاء المبنى";
            return RedirectToAction("Buildings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Buildings.Manage", "CanEdit")]
        public async Task<IActionResult> EditBuilding(EditBuildingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("Buildings");
            }

            var building = await _db.CityBuildings.FindAsync(model.ID);
            if (building == null) return NotFound();

            var old = new { building.BuildingName, building.BuildingType, building.IsActive };

            building.BuildingName = model.BuildingName;
            building.BuildingType = model.BuildingType;
            building.FloorCount = (byte)model.FloorCount;
            building.IsActive = model.IsActive;
            building.LastUpdatedAt = DateTime.UtcNow;
            building.LastUpdatedBy = CurrentUserId;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Building.Edit",
                "CityBuilding", model.ID, old,
                new { model.BuildingName, model.BuildingType, model.IsActive });

            TempData["Success"] = "تم تحديث المبنى";
            return RedirectToAction("Buildings");
        }

        [HttpGet]
        [RequirePermission("Rooms.Manage", "CanView")]
        public async Task<IActionResult> Rooms(int? buildingId = null)
        {
            var query = _db.CityRooms
                .Include(r => r.CityBuilding)
                    .ThenInclude(b => b.DormitoryCity)
                .Where(r => r.IsDeleted != true)
                .AsQueryable();

            if (buildingId.HasValue)
                query = query.Where(r => r.CityBuildingID == buildingId.Value);

            var rooms = await query
                .OrderBy(r => r.CityBuilding.DormitoryCity.Name)
                .ThenBy(r => r.CityBuilding.BuildingName)
                .ThenBy(r => r.FloorNumber)
                .ThenBy(r => r.RoomNumber)
                .Select(r => new RoomRowViewModel
                {
                    ID = r.ID,
                    RoomNumber = r.RoomNumber,
                    FloorNumber = r.FloorNumber,
                    BedsCount = r.BedsCount,
                    CurrentOccupancy = r.CurrentOccupancy,
                    RoomType = r.RoomType,
                    BuildingName = r.CityBuilding.BuildingName,
                    BuildingID = r.CityBuildingID,
                    CityName = r.CityBuilding.DormitoryCity.Name,
                    HasAC = r.HasAC == true,
                    IsActive = r.IsActive == true
                })
                .ToListAsync();

            ViewBag.Buildings = await _db.CityBuildings
                .Where(b => b.IsActive && !b.IsDeleted)
                .Include(b => b.DormitoryCity)
                .OrderBy(b => b.DormitoryCity.Name)
                .ThenBy(b => b.BuildingName)
                .ToListAsync();

            ViewBag.FilterBuildingId = buildingId;

            return View(rooms);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Rooms.Manage", "CanCreate")]
        public async Task<IActionResult> Rooms(CreateRoomViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("Rooms");

            var existing = await _db.CityRooms
                .AnyAsync(r => r.CityBuildingID == model.CityBuildingID
                            && r.RoomNumber == model.RoomNumber
                            && r.IsDeleted != true);

            if (existing)
            {
                TempData["Error"] = "رقم الغرفة موجود مسبقاً في هذا المبنى";
                return RedirectToAction("Rooms");
            }

            var room = new CityRoom
            {
                CityBuildingID = model.CityBuildingID,
                RoomNumber = model.RoomNumber,
                FloorNumber = (byte)model.FloorNumber,
                BedsCount = (byte)model.BedsCount,
                RoomType = model.RoomType,
                HasAC = model.HasAC,
                HasBalcony = model.HasBalcony,
                HasPrivateBathroom = model.HasPrivateBathroom,
                HasFridge = model.HasFridge,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };

            _db.CityRooms.Add(room);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Room.Create",
                "CityRoom", room.ID, null,
                new { room.RoomNumber, room.CityBuildingID, room.FloorNumber, room.BedsCount });

            TempData["Success"] = "تم إنشاء الغرفة";
            return RedirectToAction("Rooms");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Rooms.Manage", "CanEdit")]
        public async Task<IActionResult> EditRoom(EditRoomViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("Rooms");
            }

            var room = await _db.CityRooms.FindAsync(model.ID);
            if (room == null) return NotFound();

            var old = new { room.RoomNumber, room.FloorNumber, room.BedsCount, room.RoomType, room.IsActive };

            room.RoomNumber = model.RoomNumber;
            room.FloorNumber = (byte)model.FloorNumber;
            room.BedsCount = (byte)model.BedsCount;
            room.RoomType = model.RoomType;
            room.HasAC = model.HasAC;
            room.HasBalcony = model.HasBalcony;
            room.HasPrivateBathroom = model.HasPrivateBathroom;
            room.HasFridge = model.HasFridge;
            room.IsActive = model.IsActive;
            room.LastUpdatedAt = DateTime.UtcNow;
            room.LastUpdatedBy = CurrentUserId;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Room.Edit",
                "CityRoom", model.ID, old,
                new { model.RoomNumber, model.FloorNumber, model.BedsCount, model.IsActive });

            TempData["Success"] = "تم تحديث الغرفة";
            return RedirectToAction("Rooms");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 4. المواعيد والتعليمات
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("ApplicationSchedules.Manage", "CanView")]
        public async Task<IActionResult> Schedules()
        {
            var schedules = await _db.ApplicationSchedules
                .Include(s => s.DormitoryCity)
                .OrderByDescending(s => s.AcademicYear)
                .ThenBy(s => s.DormitoryCity.Name)
                .ToListAsync();

            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .ToListAsync();

            return View(schedules);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("ApplicationSchedules.Manage", "CanCreate")]
        public async Task<IActionResult> Schedules(CreateScheduleViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("Schedules");

            var existing = await _db.ApplicationSchedules
                .AnyAsync(s => s.DormitoryCityID == model.DormitoryCityID
                            && s.AcademicYear == model.AcademicYear);

            if (existing)
            {
                TempData["Error"] = "يوجد جدول مواعيد لهذه المدينة والعام الدراسي";
                return RedirectToAction("Schedules");
            }

            var schedule = new ApplicationSchedule
            {
                DormitoryCityID = model.DormitoryCityID,
                AcademicYear = model.AcademicYear,
                NewStudentsOpenDate = model.NewStudentsOpenDate,
                NewStudentsCloseDate = model.NewStudentsCloseDate,
                ReturningStudentsOpenDate = model.ReturningStudentsOpenDate,
                ReturningStudentsCloseDate = model.ReturningStudentsCloseDate,
                IsOpen = model.IsOpen
            };

            _db.ApplicationSchedules.Add(schedule);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Schedule.Create",
                "ApplicationSchedule", schedule.ID, null, model);

            TempData["Success"] = "تم إضافة جدول المواعيد";
            return RedirectToAction("Schedules");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("ApplicationSchedules.Manage", "CanEdit")]
        public async Task<IActionResult> EditSchedule(EditScheduleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("Schedules");
            }

            var schedule = await _db.ApplicationSchedules.FindAsync(model.ID);
            if (schedule == null) return NotFound();

            schedule.NewStudentsOpenDate = model.NewStudentsOpenDate;
            schedule.NewStudentsCloseDate = model.NewStudentsCloseDate;
            schedule.ReturningStudentsOpenDate = model.ReturningStudentsOpenDate;
            schedule.ReturningStudentsCloseDate = model.ReturningStudentsCloseDate;
            schedule.IsOpen = model.IsOpen;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Schedule.Edit",
                "ApplicationSchedule", model.ID, null, model);

            TempData["Success"] = "تم تحديث جدول المواعيد";
            return RedirectToAction("Schedules");
        }

        [HttpGet]
        [RequirePermission("Instructions.Manage", "CanView")]
        public async Task<IActionResult> Instructions()
        {
            var instructions = await _db.HousingInstructions
                .Include(i => i.HousingInstructionAttachments)
                .Include(i => i.DormitoryCity)
                .Where(i => i.IsActive == true)
                .OrderBy(i => i.SortOrder)
                .ToListAsync();

            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .ToListAsync();

            return View(instructions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Instructions.Manage", "CanCreate")]
        public async Task<IActionResult> Instructions(CreateInstructionViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("Instructions");

            var maxSort = await _db.HousingInstructions
                .MaxAsync(i => (byte?)i.SortOrder) ?? 0;

            var instruction = new HousingInstruction
            {
                DormitoryCityID = model.DormitoryCityID,
                Title = model.Title,
                Content = model.Content,
                InstructionType = model.InstructionType,
                SortOrder = (byte)(maxSort + 1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };

            _db.HousingInstructions.Add(instruction);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Instruction.Create",
                "HousingInstruction", instruction.ID, null,
                new { instruction.Title, instruction.InstructionType });

            TempData["Success"] = "تم إضافة الإرشادات";
            return RedirectToAction("Instructions");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Instructions.Manage", "CanEdit")]
        public async Task<IActionResult> EditInstruction(EditInstructionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("Instructions");
            }

            var inst = await _db.HousingInstructions.FindAsync(model.ID);
            if (inst == null) return NotFound();

            inst.Title = model.Title;
            inst.Content = model.Content;
            inst.InstructionType = model.InstructionType;
            inst.SortOrder = (byte)model.SortOrder;
            inst.IsActive = model.IsActive;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Instruction.Edit",
                "HousingInstruction", model.ID, null,
                new { model.Title, model.InstructionType, model.IsActive });

            TempData["Success"] = "تم تحديث الإرشادات";
            return RedirectToAction("Instructions");
        }

        [HttpGet]
        [RequirePermission("Instructions.Manage", "CanEdit")]
        public async Task<IActionResult> InstructionAttachments(int instructionId)
        {
            var instruction = await _db.HousingInstructions
                .Include(i => i.HousingInstructionAttachments)
                .FirstOrDefaultAsync(i => i.ID == instructionId);

            if (instruction == null) return NotFound();

            ViewBag.Instruction = instruction;
            return View(instruction.HousingInstructionAttachments.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Instructions.Manage", "CanEdit")]
        public async Task<IActionResult> UploadInstructionAttachment(int instructionId, IFormFile file, string? fileName)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "يرجى اختيار ملف";
                return RedirectToAction("InstructionAttachments", new { instructionId });
            }

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "instructions");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName);
            var safeName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsDir, safeName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new HousingInstructionAttachment
            {
                HousingInstructionID = instructionId,
                FileName = fileName ?? file.FileName,
                FilePath = $"/uploads/instructions/{safeName}",
                FileType = ext.TrimStart('.'),
                IsActive = true
            };

            _db.HousingInstructionAttachments.Add(attachment);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "InstructionAttachment.Upload",
                "HousingInstructionAttachment", attachment.ID, null,
                new { attachment.FileName, instructionId });

            TempData["Success"] = "تم رفع الملف";
            return RedirectToAction("InstructionAttachments", new { instructionId });
        }

        [HttpPost]
        [RequirePermission("Instructions.Manage", "CanDelete")]
        public async Task<IActionResult> DeleteInstructionAttachment(int id)
        {
            var attachment = await _db.HousingInstructionAttachments.FindAsync(id);
            if (attachment == null) return Json(new { success = false });

            var instId = attachment.HousingInstructionID;

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            await _audit.LogAsync(CurrentUserId, "Staff", "Attachment.Delete",
                "HousingInstructionAttachment", id,
                new { attachment.FileName, attachment.FilePath }, null);

            _db.HousingInstructionAttachments.Remove(attachment);
            await _db.SaveChangesAsync();

            return Json(new { success = true, instructionId = instId });
        }

        [HttpGet]
        [RequirePermission("AppConfig.Manage", "CanView")]
        public async Task<IActionResult> Announcements()
        {
            var announcements = await _db.Announcements
                .Include(a => a.AnnouncementAttachments)
                .Include(a => a.CreatedByNavigation)
                .Include(a => a.DormitoryCity)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .ToListAsync();

            return View(announcements);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("AppConfig.Manage", "CanCreate")]
        public async Task<IActionResult> Announcements(CreateAnnouncementViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("Announcements");

            var announcement = new Announcement
            {
                Title = model.Title,
                Body = model.Body,
                AnnouncementType = model.AnnouncementType,
                DormitoryCityID = model.DormitoryCityID,
                TargetAudience = model.TargetAudience,
                IsPublished = model.PublishNow,
                PublishedAt = model.PublishNow ? DateTime.UtcNow : null,
                ExpiresAt = model.ExpiresAt,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };

            _db.Announcements.Add(announcement);
            await _db.SaveChangesAsync();

            if (model.Files != null && model.Files.Any())
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "announcements");
                Directory.CreateDirectory(uploadsDir);

                foreach (var file in model.Files)
                {
                    if (file.Length == 0) continue;

                    var ext = Path.GetExtension(file.FileName);
                    var safeName = $"{Guid.NewGuid()}{ext}";
                    var fullPath = Path.Combine(uploadsDir, safeName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _db.AnnouncementAttachments.Add(new AnnouncementAttachment
                    {
                        AnnouncementID = announcement.ID,
                        FileName = file.FileName,
                        FilePath = $"/uploads/announcements/{safeName}"
                    });
                }

                await _db.SaveChangesAsync();
            }

            await _audit.LogAsync(CurrentUserId, "Staff", "Announcement.Create",
                "Announcement", announcement.ID, null,
                new { announcement.Title, announcement.AnnouncementType, announcement.IsPublished });

            TempData["Success"] = "تم إنشاء الإعلان";
            return RedirectToAction("Announcements");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("AppConfig.Manage", "CanEdit")]
        public async Task<IActionResult> EditAnnouncement(EditAnnouncementViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("Announcements");
            }

            var announcement = await _db.Announcements.FindAsync(model.ID);
            if (announcement == null) return NotFound();

            var old = new { announcement.Title, announcement.IsPublished };

            announcement.Title = model.Title;
            announcement.Body = model.Body;
            announcement.AnnouncementType = model.AnnouncementType;
            announcement.DormitoryCityID = model.DormitoryCityID;
            announcement.TargetAudience = model.TargetAudience;
            announcement.ExpiresAt = model.ExpiresAt;

            if (model.PublishNow && announcement.IsPublished != true)
            {
                announcement.IsPublished = true;
                announcement.PublishedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Announcement.Edit",
                "Announcement", model.ID, old,
                new { model.Title, model.AnnouncementType });

            TempData["Success"] = "تم تحديث الإعلان";
            return RedirectToAction("Announcements");
        }

        [HttpPost]
        [RequirePermission("AppConfig.Manage", "CanEdit")]
        public async Task<IActionResult> TogglePublishAnnouncement(int id)
        {
            var announcement = await _db.Announcements.FindAsync(id);
            if (announcement == null) return Json(new { success = false });

            var oldPublished = announcement.IsPublished;
            announcement.IsPublished = !announcement.IsPublished;
            announcement.PublishedAt = announcement.IsPublished == true ? DateTime.UtcNow : null;

            await _audit.LogAsync(CurrentUserId, "Staff", "Announcement.TogglePublish",
                "Announcement", id,
                new { IsPublished = oldPublished },
                new { IsPublished = announcement.IsPublished });

            await _db.SaveChangesAsync();

            return Json(new { success = true, isPublished = announcement.IsPublished });
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 5. الكروكي — خريطة المبنى
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("Buildings.Manage", "CanView")]
        public async Task<IActionResult> BuildingLayout(int id)
        {
            var building = await _db.CityBuildings
                .Include(b => b.DormitoryCity)
                .Include(b => b.CityRooms)
                .FirstOrDefaultAsync(b => b.ID == id && !b.IsDeleted);

            if (building == null)
                return Content("Building Not Found");

            var layout = new BuildingLayoutViewModel
            {
                BuildingID = building.ID,
                BuildingName = building.BuildingName,
                CityName = building.DormitoryCity.Name,
                FloorCount = building.FloorCount,
                TotalBeds = building.CityRooms.Sum(r => r.BedsCount),
                OccupiedBeds = building.CityRooms.Sum(r => r.CurrentOccupancy)
            };

            var floors = building.CityRooms
                .GroupBy(r => r.FloorNumber)
                .OrderBy(g => g.Key)
                .Select(g => new FloorLayoutViewModel
                {
                    FloorNumber = g.Key,
                    Rooms = g.OrderBy(r => r.RoomNumber).Select(r => new RoomLayoutViewModel
                    {
                        RoomID = r.ID,
                        RoomNumber = r.RoomNumber,
                        BedsCount = r.BedsCount,
                        CurrentOccupancy = r.CurrentOccupancy,
                        AvailableBeds = r.BedsCount - r.CurrentOccupancy,
                        IsFull = r.CurrentOccupancy >= r.BedsCount,
                        RoomType = r.RoomType ?? "Standard"
                    }).ToList()
                }).ToList();

            layout.Floors = floors;

            return View(layout);
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 6. Villages
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("Villages.Manage", "CanView")]
        public async Task<IActionResult> Villages(int? cityId = null)
        {
            var query = _db.Villages
                .Include(v => v.DormitoryCity)
                .AsQueryable();

            if (cityId.HasValue)
                query = query.Where(v => v.DormitoryCityID == cityId.Value);

            var villages = await query
                .OrderBy(v => v.DormitoryCity.Name).ThenBy(v => v.Name)
                .Select(v => new VillageViewModel
                {
                    ID = v.ID,
                    Name = v.Name,
                    DormitoryCityID = v.DormitoryCityID,
                    CityName = v.DormitoryCity.Name,
                    IsActive = v.IsActive,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();

            ViewBag.Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted).ToListAsync();
            ViewBag.FilterCityId = cityId;
            return View(villages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Villages.Manage", "CanCreate")]
        public async Task<IActionResult> Villages(CreateVillageViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("Villages");

            var village = new Village
            {
                DormitoryCityID = model.DormitoryCityID,
                Name = model.Name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };

            _db.Villages.Add(village);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Village.Create", "Village", village.ID,
                null, new { village.Name, village.DormitoryCityID });

            TempData["Success"] = "تم إضافة القرية";
            return RedirectToAction("Villages");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Villages.Manage", "CanEdit")]
        public async Task<IActionResult> EditVillage(EditVillageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("Villages");
            }

            var village = await _db.Villages.FindAsync(model.ID);
            if (village == null) return NotFound();

            var oldName = village.Name;
            village.Name = model.Name;
            village.IsActive = model.IsActive;
            village.LastUpdatedAt = DateTime.UtcNow;
            village.LastUpdatedBy = CurrentUserId;

            await _db.SaveChangesAsync();
            await _audit.LogAsync(CurrentUserId, "Staff", "Village.Edit", "Village", model.ID,
                new { Name = oldName }, new { model.Name, model.IsActive });

            TempData["Success"] = "تم تحديث القرية";
            return RedirectToAction("Villages");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 7. Housing Types
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("HousingTypes.Manage", "CanView")]
        public async Task<IActionResult> HousingTypes()
        {
            var types = await _db.HousingTypes
                .OrderBy(t => t.Name)
                .Select(t => new HousingTypeViewModel
                {
                    ID = t.ID,
                    Name = t.Name,
                    Description = t.Description,
                    IsActive = t.IsActive
                })
                .ToListAsync();

            return View(types);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("HousingTypes.Manage", "CanCreate")]
        public async Task<IActionResult> HousingTypes(CreateHousingTypeViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("HousingTypes");

            var type = new HousingType { Name = model.Name, Description = model.Description, IsActive = true };
            _db.HousingTypes.Add(type);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "HousingType.Create", "HousingType", type.ID,
                null, new { type.Name });

            TempData["Success"] = "تم إضافة نوع السكن";
            return RedirectToAction("HousingTypes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("HousingTypes.Manage", "CanEdit")]
        public async Task<IActionResult> EditHousingType(EditHousingTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("HousingTypes");
            }

            var type = await _db.HousingTypes.FindAsync(model.ID);
            if (type == null) return NotFound();

            type.Name = model.Name;
            type.Description = model.Description;
            type.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "HousingType.Edit", "HousingType", model.ID,
                null, new { model.Name, model.IsActive });

            TempData["Success"] = "تم تحديث نوع السكن";
            return RedirectToAction("HousingTypes");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 8. Meal Types
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("MealTypes.Manage", "CanView")]
        public async Task<IActionResult> MealTypes()
        {
            var types = await _db.MealTypes
                .OrderBy(t => t.Name)
                .Select(t => new MealTypeViewModel
                {
                    ID = t.ID,
                    Name = t.Name,
                    Description = t.Description,
                    IsActive = t.IsActive
                })
                .ToListAsync();

            return View(types);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("MealTypes.Manage", "CanCreate")]
        public async Task<IActionResult> MealTypes(CreateMealTypeViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("MealTypes");

            var type = new MealType { Name = model.Name, Description = model.Description, IsActive = true };
            _db.MealTypes.Add(type);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "MealType.Create", "MealType", type.ID,
                null, new { type.Name });

            TempData["Success"] = "تم إضافة نوع الوجبة";
            return RedirectToAction("MealTypes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("MealTypes.Manage", "CanEdit")]
        public async Task<IActionResult> EditMealType(EditMealTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("MealTypes");
            }

            var type = await _db.MealTypes.FindAsync(model.ID);
            if (type == null) return NotFound();

            type.Name = model.Name;
            type.Description = model.Description;
            type.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "MealType.Edit", "MealType", model.ID,
                null, new { model.Name, model.IsActive });

            TempData["Success"] = "تم تحديث نوع الوجبة";
            return RedirectToAction("MealTypes");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 9. Fee Types
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("FeeTypes.Manage", "CanView")]
        public async Task<IActionResult> FeeTypes()
        {
            var types = await _db.FeeTypes
                .OrderBy(t => t.FeeCategory).ThenBy(t => t.Name)
                .Select(t => new FeeTypeViewModel
                {
                    ID = t.ID,
                    Name = t.Name,
                    Description = t.Description,
                    FeeCategory = t.FeeCategory,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return View(types);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("FeeTypes.Manage", "CanCreate")]
        public async Task<IActionResult> FeeTypes(CreateFeeTypeViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("FeeTypes");

            var type = new FeeType
            {
                Name = model.Name,
                Description = model.Description,
                FeeCategory = model.FeeCategory,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };
            _db.FeeTypes.Add(type);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "FeeType.Create", "FeeType", type.ID,
                null, new { type.Name, type.FeeCategory });

            TempData["Success"] = "تم إضافة نوع الرسم";
            return RedirectToAction("FeeTypes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("FeeTypes.Manage", "CanEdit")]
        public async Task<IActionResult> EditFeeType(EditFeeTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("FeeTypes");
            }

            var type = await _db.FeeTypes.FindAsync(model.ID);
            if (type == null) return NotFound();

            type.Name = model.Name;
            type.Description = model.Description;
            type.FeeCategory = model.FeeCategory;
            type.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "FeeType.Edit", "FeeType", model.ID,
                null, new { model.Name, model.FeeCategory, model.IsActive });

            TempData["Success"] = "تم تحديث نوع الرسم";
            return RedirectToAction("FeeTypes");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 10. Fee Configurations
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("FeeConfigurations.Manage", "CanView")]
        public async Task<IActionResult> FeeConfigurations()
        {
            var configs = await _db.FeeConfigurations
                .Include(f => f.FeeType)
                .Include(f => f.DormitoryCity)
                .OrderBy(f => f.FeeType.Name)
                .Select(f => new FeeConfigurationViewModel
                {
                    ID = f.ID,
                    FeeTypeID = f.FeeTypeID,
                    FeeTypeName = f.FeeType.Name,
                    DormitoryCityID = f.DormitoryCityID,
                    CityName = f.DormitoryCity != null ? f.DormitoryCity.Name : null,
                    Amount = f.Amount,
                    AcademicYear = f.AcademicYear,
                    IsActive = f.IsActive
                })
                .ToListAsync();

            ViewBag.FeeTypes = await _db.FeeTypes.Where(t => t.IsActive).ToListAsync();
            ViewBag.Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted).ToListAsync();
            return View(configs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("FeeConfigurations.Manage", "CanCreate")]
        public async Task<IActionResult> FeeConfigurations(CreateFeeConfigurationViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("FeeConfigurations");

            var config = new FeeConfiguration
            {
                FeeTypeID = model.FeeTypeID,
                DormitoryCityID = model.DormitoryCityID,
                Amount = model.Amount,
                AcademicYear = model.AcademicYear ?? GetCurrentAcademicYear(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };
            _db.FeeConfigurations.Add(config);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "FeeConfig.Create", "FeeConfiguration", config.ID,
                null, new { config.FeeTypeID, config.Amount, config.AcademicYear });

            TempData["Success"] = "تم إضافة تكوين الرسم";
            return RedirectToAction("FeeConfigurations");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("FeeConfigurations.Manage", "CanEdit")]
        public async Task<IActionResult> EditFeeConfiguration(EditFeeConfigurationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("FeeConfigurations");
            }

            var config = await _db.FeeConfigurations.FindAsync(model.ID);
            if (config == null) return NotFound();

            var oldAmount = config.Amount;
            config.Amount = model.Amount;
            config.IsActive = model.IsActive;
            config.LastUpdatedAt = DateTime.UtcNow;
            config.LastUpdatedBy = CurrentUserId;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "FeeConfig.Edit", "FeeConfiguration", model.ID,
                new { Amount = oldAmount }, new { model.Amount, model.IsActive });

            TempData["Success"] = "تم تحديث تكوين الرسم";
            return RedirectToAction("FeeConfigurations");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 11. Countries
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("Countries.Manage", "CanView")]
        public async Task<IActionResult> Countries()
        {
            var countries = await _db.Countries
                .OrderBy(c => c.Name)
                .Select(c => new CountryViewModel
                {
                    ID = c.ID,
                    Name = c.Name,
                    NameAr = c.NameAr,
                    Code = c.Code,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return View(countries);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Countries.Manage", "CanCreate")]
        public async Task<IActionResult> Countries(CreateCountryViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("Countries");

            var country = new Country
            {
                Name = model.Name,
                NameAr = model.NameAr,
                Code = model.Code,
                IsActive = true
            };
            _db.Countries.Add(country);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Country.Create", "Country", country.ID,
                null, new { country.Name, country.Code });

            TempData["Success"] = "تم إضافة الدولة";
            return RedirectToAction("Countries");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Countries.Manage", "CanEdit")]
        public async Task<IActionResult> EditCountry(EditCountryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("Countries");
            }

            var country = await _db.Countries.FindAsync(model.ID);
            if (country == null) return NotFound();

            country.Name = model.Name;
            country.NameAr = model.NameAr;
            country.Code = model.Code;
            country.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Country.Edit", "Country", model.ID,
                null, new { model.Name, model.IsActive });

            TempData["Success"] = "تم تحديث الدولة";
            return RedirectToAction("Countries");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 12. Student Categories
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("StudentCategories.Manage", "CanView")]
        public async Task<IActionResult> StudentCategories()
        {
            var categories = await _db.StudentCategories
                .OrderBy(c => c.Name)
                .Select(c => new StudentCategoryViewModel
                {
                    ID = c.ID,
                    Name = c.Name,
                    Description = c.Description,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("StudentCategories.Manage", "CanCreate")]
        public async Task<IActionResult> StudentCategories(CreateStudentCategoryViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("StudentCategories");

            var cat = new StudentCategory { Name = model.Name, Description = model.Description, IsActive = true };
            _db.StudentCategories.Add(cat);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "StudentCategory.Create", "StudentCategory", cat.ID,
                null, new { cat.Name });

            TempData["Success"] = "تم إضافة التصنيف";
            return RedirectToAction("StudentCategories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("StudentCategories.Manage", "CanEdit")]
        public async Task<IActionResult> EditStudentCategory(EditStudentCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("StudentCategories");
            }

            var cat = await _db.StudentCategories.FindAsync(model.ID);
            if (cat == null) return NotFound();

            cat.Name = model.Name;
            cat.Description = model.Description;
            cat.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "StudentCategory.Edit", "StudentCategory", model.ID,
                null, new { model.Name, model.IsActive });

            TempData["Success"] = "تم تحديث التصنيف";
            return RedirectToAction("StudentCategories");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 13. Application Configuration
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("AppConfig.Manage", "CanView")]
        public async Task<IActionResult> AppConfig(string? category = null)
        {
            var query = _db.ApplicationConfigurations.AsQueryable();
            if (!string.IsNullOrEmpty(category))
                query = query.Where(c => c.Category == category);

            var configs = await query
                .OrderBy(c => c.Category).ThenBy(c => c.ConfigKey)
                .Select(c => new AppConfigViewModel
                {
                    ID = c.ID,
                    ConfigKey = c.ConfigKey,
                    ConfigValue = c.ConfigValue,
                    Description = c.Description,
                    Category = c.Category,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            ViewBag.Categories = await _db.ApplicationConfigurations
                .Select(c => c.Category).Distinct().ToListAsync();

            return View(configs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("AppConfig.Manage", "CanCreate")]
        public async Task<IActionResult> AppConfig(CreateAppConfigViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("AppConfig");

            var exists = await _db.ApplicationConfigurations.AnyAsync(c => c.ConfigKey == model.ConfigKey);
            if (exists)
            {
                TempData["Error"] = "المفتاح موجود مسبقاً";
                return RedirectToAction("AppConfig");
            }

            var config = new ApplicationConfiguration
            {
                ConfigKey = model.ConfigKey,
                ConfigValue = model.ConfigValue,
                Description = model.Description,
                Category = model.Category,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };
            _db.ApplicationConfigurations.Add(config);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "AppConfig.Create", "ApplicationConfiguration",
                config.ID, null, new { config.ConfigKey, config.ConfigValue, config.Category });

            TempData["Success"] = "تم إضافة الإعداد";
            return RedirectToAction("AppConfig");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("AppConfig.Manage", "CanEdit")]
        public async Task<IActionResult> EditAppConfig(EditAppConfigViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("AppConfig");
            }

            var config = await _db.ApplicationConfigurations.FindAsync(model.ID);
            if (config == null) return NotFound();

            var oldValue = config.ConfigValue;
            config.ConfigValue = model.ConfigValue;
            config.Description = model.Description;
            config.IsActive = model.IsActive;
            config.LastUpdatedAt = DateTime.UtcNow;
            config.LastUpdatedBy = CurrentUserId;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "AppConfig.Edit", "ApplicationConfiguration",
                model.ID, new { ConfigValue = oldValue }, new { model.ConfigValue, model.IsActive });

            TempData["Success"] = "تم تحديث الإعداد";
            return RedirectToAction("AppConfig");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 14. Roles Management
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("Roles.Manage", "CanView")]
        public async Task<IActionResult> Roles()
        {
            var roles = await _db.Roles
                .OrderBy(r => r.Name)
                .Select(r => new RoleViewModel
                {
                    ID = r.ID,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    UserCount = r.UserRoles.Count
                })
                .ToListAsync();

            return View(roles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Roles.Manage", "CanCreate")]
        public async Task<IActionResult> Roles(CreateRoleViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("Roles");

            var exists = await _db.Roles.AnyAsync(r => r.Name == model.Name);
            if (exists)
            {
                TempData["Error"] = "اسم الدور موجود مسبقاً";
                return RedirectToAction("Roles");
            }

            var role = new Role
            {
                Name = model.Name,
                Description = model.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Role.Create", "Role", role.ID,
                null, new { role.Name });

            TempData["Success"] = "تم إضافة الدور";
            return RedirectToAction("Roles");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Roles.Manage", "CanEdit")]
        public async Task<IActionResult> EditRole(EditRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction("Roles");
            }

            var role = await _db.Roles.FindAsync(model.ID);
            if (role == null) return NotFound();

            role.Name = model.Name;
            role.Description = model.Description;
            role.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Role.Edit", "Role", model.ID,
                null, new { model.Name, model.IsActive });

            TempData["Success"] = "تم تحديث الدور";
            return RedirectToAction("Roles");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 15. Role Permissions Assignment
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("Permissions.Manage", "CanView")]
        public async Task<IActionResult> RolePermissions(int roleId)
        {
            var role = await _db.Roles.FindAsync(roleId);
            if (role == null) return NotFound();

            var allGroups = await _db.PermissionGroups
                .Include(g => g.Permissions)
                .OrderBy(g => g.GroupName)
                .ToListAsync();

            var currentPerms = await _db.RolePermissions
                .Where(rp => rp.RoleID == roleId)
                .ToListAsync();

            var permDict = currentPerms.ToDictionary(p => p.PermissionID);

            var groups = allGroups.Select(g => new PermissionGroupViewModel
            {
                GroupID = g.ID,
                GroupName = g.GroupName,
                Description = g.Description,
                Permissions = g.Permissions.Select(p => new PermissionItemViewModel
                {
                    PermissionID = p.ID,
                    PermissionKey = p.PermissionKey,
                    DisplayName = p.DisplayName,
                    Category = p.Category,
                    CanView = permDict.TryGetValue(p.ID, out var rp) && rp.CanView,
                    CanCreate = permDict.TryGetValue(p.ID, out rp) && rp.CanCreate,
                    CanEdit = permDict.TryGetValue(p.ID, out rp) && rp.CanEdit,
                    CanDelete = permDict.TryGetValue(p.ID, out rp) && rp.CanDelete,
                }).ToList()
            }).ToList();

            ViewBag.Role = role;
            return View(groups);
        }

        [HttpPost]
        [RequirePermission("Permissions.Manage", "CanCreate")]
        public async Task<IActionResult> RolePermissions([FromBody] SaveRolePermissionsRequest request)
        {
            var existing = await _db.RolePermissions
                .Where(rp => rp.RoleID == request.RoleID)
                .ToListAsync();

            var existingDict = existing.ToDictionary(rp => rp.PermissionID);
            _db.RolePermissions.RemoveRange(existing);

            foreach (var item in request.Permissions)
            {
                if (item.CanView || item.CanCreate || item.CanEdit || item.CanDelete)
                {
                    _db.RolePermissions.Add(new RolePermission
                    {
                        RoleID = request.RoleID,
                        PermissionID = item.PermissionID,
                        CanView = item.CanView,
                        CanCreate = item.CanCreate,
                        CanEdit = item.CanEdit,
                        CanDelete = item.CanDelete
                    });
                }
            }

            await _db.SaveChangesAsync();
            await _audit.LogAsync(CurrentUserId, "Staff", "RolePermissions.Update", "RolePermission", request.RoleID);

            return Json(new { success = true });
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 16. Audit Log
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [RequirePermission("AuditLog.View", "CanView")]
        public async Task<IActionResult> AuditLog(int page = 1)
        {
            var query = _db.AuditLogs.AsQueryable();
            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * 50)
                .Take(50)
                .ToListAsync();

            var userIds = logs.Select(l => l.UserID).Distinct().ToList();
            var userNames = await _db.SystemUsers
                .Where(u => userIds.Contains(u.ID))
                .ToDictionaryAsync(u => u.ID, u => u.Name);

            var logVms = logs.Select(l => new AuditLogRowViewModel
            {
                ID = l.ID,
                UserID = l.UserID,
                UserType = l.UserType,
                UserDisplayName = userNames.GetValueOrDefault(l.UserID, $"#{l.UserID}"),
                Action = l.Action,
                ActionDisplay = l.Action,
                TableName = l.TableName,
                RecordID = l.RecordID,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                CreatedAt = l.CreatedAt
            }).ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / 50.0);
            return View(logVms);
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 17. Advanced Student Operations
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Students.Manage", "CanEdit")]
        public async Task<IActionResult> CorrectNationalId(CorrectNationalIdViewModel model)
        {
            var student = await _db.Students.FindAsync(model.StudentID);
            if (student == null) return Json(new { success = false, message = "الطالب غير موجود" });

            var oldValue = student.NationalID;
            var exists = await _db.Students.AnyAsync(s => s.NationalID == model.NewNationalID && s.ID != model.StudentID);
            if (exists) return Json(new { success = false, message = "الرقم القومي مستخدم من قبل طالب آخر" });

            student.NationalID = model.NewNationalID;
            student.LastUpdatedAt = DateTime.UtcNow;
            student.LastUpdatedBy = CurrentUserId;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Student.CorrectNationalID", "Student", model.StudentID,
                new { NationalID = oldValue }, new { NationalID = model.NewNationalID, Reason = model.Reason });

            return Json(new { success = true, message = "تم تصحيح الرقم القومي" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Students.Manage", "CanEdit")]
        public async Task<IActionResult> ChangeStudentNumber(ChangeStudentNumberViewModel model)
        {
            var student = await _db.Students.FindAsync(model.StudentID);
            if (student == null) return Json(new { success = false, message = "الطالب غير موجود" });

            var oldValue = student.StudentCode;
            student.StudentCode = model.NewStudentCode;
            student.LastUpdatedAt = DateTime.UtcNow;
            student.LastUpdatedBy = CurrentUserId;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Student.ChangeNumber", "Student", model.StudentID,
                new { StudentCode = oldValue }, new { StudentCode = model.NewStudentCode, Reason = model.Reason });

            return Json(new { success = true, message = "تم تغيير رقم الجلوس" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Coordination.Manage", "CanEdit")]
        public async Task<IActionResult> ReverseAcceptance(ReverseAcceptanceViewModel model)
        {
            var app = await _db.Applications
                .FirstOrDefaultAsync(a => a.StudentID == model.StudentID && a.Status == "Accepted");
            if (app == null) return Json(new { success = false, message = "لا يوجد قبول نشط لهذا الطالب" });

            var oldStatus = app.Status;
            app.Status = "Pending";
            app.RejectionReason = model.Reason;
            app.LastUpdatedAt = DateTime.UtcNow;
            app.LastUpdatedBy = CurrentUserId;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Application.ReverseAcceptance", "Application", app.ID,
                new { Status = oldStatus }, new { Status = "Pending", Reason = model.Reason });

            return Json(new { success = true, message = "تم إلغاء القبول" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Students.Manage", "CanEdit")]
        public async Task<IActionResult> TransferUniversity(TransferUniversityViewModel model)
        {
            var student = await _db.Students.FindAsync(model.StudentID);
            if (student == null) return Json(new { success = false, message = "الطالب غير موجود" });

            var oldUniv = student.Faculty;
            var newUniv = await _db.Universities.FindAsync(model.NewUniversityID);
            if (newUniv == null) return Json(new { success = false, message = "الجامعة غير موجودة" });

            student.Faculty = newUniv.Name;
            student.LastUpdatedAt = DateTime.UtcNow;
            student.LastUpdatedBy = CurrentUserId;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Student.TransferUniversity", "Student", model.StudentID,
                new { Faculty = oldUniv }, new { Faculty = newUniv.Name, Reason = model.Reason });

            return Json(new { success = true, message = "تم تحويل الطالب" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Students.Manage", "CanEdit")]
        public async Task<IActionResult> ResetStudentPassword(int studentId)
        {
            var login = await _db.StudentLogins.FirstOrDefaultAsync(l => l.StudentID == studentId);
            if (login == null) return Json(new { success = false, message = "لا يوجد حساب للطالب" });

            login.PasswordHash = _passwordService.HashPassword(login.Username);
            login.MustChangePassword = true;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Student.ResetPassword", "StudentLogin", studentId,
                null, new { ResetTo = "NationalID" });

            return Json(new { success = true, message = "تم إعادة تعيين كلمة المرور لرقم القومي" });
        }
    }
}
