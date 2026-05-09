using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Admin;

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

        public AdminController(
            AssuitDbContext db,
            IPermissionService perm,
            IUniversityApiService api,
            IAuditService audit,
            IEmailService email)
        {
            _db = db;
            _perm = perm;
            _api = api;
            _audit = audit;
            _email = email;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        private string GetCurrentAcademicYear()
        {
            var year = DateTime.Now.Year;
            return DateTime.Now.Month >= 9 ? $"{year}-{year + 1}" : $"{year - 1}-{year}";
        }

        public  IActionResult Index()
        {
            return View();
        }



       // ──────────────────────────────────────────────────────────────────────────────
       // 1. إدارة الطلبات
       // ──────────────────────────────────────────────────────────────────────────────

       [HttpGet]
        public async Task<IActionResult> PendingApplications(
            string? status = null,
            string? studentType = null,
            int? cityId = null,
            string? faculty = null,
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

            return View(apps);
        }

        [HttpGet]
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
        public async Task<IActionResult> AllApplications(
            string? status = null,
            string? studentType = null,
            int? cityId = null,
            string? faculty = null,
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
        public async Task<IActionResult> Students(
            string? search = null,
            string? faculty = null,
            string? gender = null,
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
                        .FirstOrDefault()!
                })
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);

            return View(students);
        }

        [HttpGet]
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

        // ──────────────────────────────────────────────────────────────────────────────
        // 3. المدن والمباني
        // ──────────────────────────────────────────────────────────────────────────────

        [HttpGet]
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
        public async Task<IActionResult> BuildingLayout(int id)
        {
            var building = await _db.CityBuildings
                .Include(b => b.DormitoryCity)
                .Include(b => b.CityRooms)
                .FirstOrDefaultAsync(b => b.ID == id && !b.IsDeleted);

            if (building == null) return NotFound();

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
    }
}
