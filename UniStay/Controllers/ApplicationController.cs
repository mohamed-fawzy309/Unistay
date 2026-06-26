using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Application;

namespace UniStay.Controllers
{
    public class ApplicationController : Controller
    {
        private readonly AssuitDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public ApplicationController(
            AssuitDbContext context,
            IPasswordService passwordService,
            IEmailService emailService,
            IAuditService auditService)
        {
            _context = context;
            _passwordService = passwordService;
            _emailService = emailService;
            _auditService = auditService;
        }

        // GET: /Application/Apply
        [HttpGet("Application/Apply")]
        public IActionResult Apply()
        {
            ViewBag.CurrentYear = DateTime.Now.Year;
            return View(new ApplicationViewModel());
        }

        /// <summary>
        /// Handles student housing application submission with transaction safety,
        /// validation-first ordering, and proper error recovery.
        /// </summary>
        [HttpPost("Application/Apply")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplicationViewModel model)
        {
            #region === PHASE 1: VALIDATION (no side effects) ===

            if (!ModelState.IsValid)
                return View(model);

            // -- Photo validation --
            if (model.Photo == null || model.Photo.Length == 0)
            {
                ModelState.AddModelError("Photo", "الصورة الشخصية مطلوبة");
                return View(model);
            }
            if (model.Photo.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("Photo", "حجم الصورة يجب ألا يتجاوز 2 ميغابايت");
                return View(model);
            }
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("Photo", "الصورة يجب أن تكون بصيغة JPG أو PNG");
                return View(model);
            }

            // -- Password confirmation --
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "كلمتا المرور غير متطابقتين");
                return View(model);
            }

            // -- Email uniqueness --
            var emailExists = await _context.Students
                .AnyAsync(s => s.Email == model.Email && s.NationalID != model.NationalID && s.IsDeleted != true);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم بالفعل من قبل طالب آخر");
                return View(model);
            }

            // -- Calculate distance from university --
            model.DistanceFromUniv = CalculateDistance(model.Governorate, model.City);

            #endregion

            #region === PHASE 2: LOOKUPS & PRE-CHECKS ===

            // -- Existing student or new --
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.NationalID == model.NationalID);
            bool isNewStudent = student == null;

            // -- Duplicate application check (BEFORE any writes) --
            string currentAcademicYear = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}";
            if (student != null)
            {
                var existingApplication = await _context.Applications
                    .FirstOrDefaultAsync(a => a.StudentID == student.ID
                        && a.AcademicYear == currentAcademicYear
                        && a.Status != "Cancelled"
                        && a.Status != "Rejected");
                if (existingApplication != null)
                {
                    ModelState.AddModelError("", "لديك طلب مقدّم بالفعل لهذا العام الدراسي. يمكنك متابعة حالة طلبك من صفحة الاستعلام.");
                    return View(model);
                }
            }

            // -- Dormitory availability check (deterministic: order by ID) --
            var dormitoryCity = await _context.DormitoryCities
                .Where(d => d.IsActive == true && d.IsDeleted == false
                    && (d.CityType == (model.Gender == "Male" ? "Male" : "Female")
                        || d.CityType == "Mixed"))
                .OrderBy(d => d.ID)
                .FirstOrDefaultAsync();
            if (dormitoryCity == null)
            {
                ModelState.AddModelError("", "لا توجد مدينة جامعية متاحة حالياً. يرجى المحاولة لاحقاً أو التواصل مع الإدارة.");
                return View(model);
            }

            // -- Existing login check --
            int? existingStudentId = student?.ID;
            var existingLogin = existingStudentId != null
                ? await _context.StudentLogins.FirstOrDefaultAsync(l => l.StudentID == existingStudentId)
                : null;

            #endregion

            #region === PHASE 3: EXECUTION STRATEGY (retry-safe DB + file operations) ===

            // SqlServerRetryingExecutionStrategy conflicts with manual BeginTransactionAsync().
            // Using CreateExecutionStrategy().ExecuteAsync() lets EF manage both retries and
            // the implicit transaction as a single retriable unit.
            var strategy = _context.Database.CreateExecutionStrategy();

            // Declared here so both the strategy lambda and the outer scope can access them
            Application application = null!;
            string? savedFilePath = null;
            string? savedPhotoPath = null;

            await strategy.ExecuteAsync(async () =>
            {
                // -- 3a. Create / update Student record --
                // Idempotent: if retried, student already exists → skip
                if (student == null)
                {
                    student = new Student
                    {
                        NationalID = model.NationalID,
                        FullName = model.FullName,
                        Gender = model.Gender,
                        BirthDate = model.BirthDate,
                        Religion = model.Religion,
                        Nationality = model.Nationality,
                        Phone = model.Phone,
                        Email = model.Email,
                        Faculty = model.Faculty,
                        AcademicYear = (byte?)model.AcademicYear,
                        GradePercentage = model.GradePercentage,
                        Governorate = model.Governorate,
                        Markaz = model.Markaz,
                        City = model.City,
                        Address = model.Address,
                        DistanceFromUniv = model.DistanceFromUniv,
                        HasFamilyAbroad = model.HasFamilyAbroad,
                        HasMedicalCondition = model.HasMedicalCondition,
                        MedicalDescription = model.MedicalDescription,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();
                }

                // -- 3b. Guardian records --
                if (!model.IsFatherDeceased)
                {
                    var father = new Guardian
                    {
                        StudentID = student.ID,
                        GuardianType = "Father",
                        FullName = model.FatherName,
                        Job = model.FatherJob,
                        Address = model.FatherAddress,
                        IsDeceased = false
                    };
                    _context.Guardians.Add(father);
                }

                if (model.IsFatherDeceased && !string.IsNullOrEmpty(model.GuardianName))
                {
                    var guardian = new Guardian
                    {
                        StudentID = student.ID,
                        GuardianType = "Other",
                        FullName = model.GuardianName,
                        NationalID = model.GuardianNationalID,
                        Address = model.GuardianAddress
                    };
                    _context.Guardians.Add(guardian);
                }

                // -- 3c. Application record --
                application = new Application
                {
                    StudentID = student.ID,
                    DormitoryCityID = dormitoryCity.ID,
                    AcademicYear = currentAcademicYear,
                    StudentType = model.IsReturningStudent ? "Returning" : "New",
                    HousingType = model.HousingType,
                    HasSpecialNeeds = model.HasMedicalCondition,
                    SpecialNeedsDescription = model.MedicalDescription,
                    MealSubscription = model.MealSubscription,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Applications.Add(application);

                // -- 3d. Student login (only if first time) --
                if (existingLogin == null)
                {
                    var studentLogin = new StudentLogin
                    {
                        StudentID = student.ID,
                        Username = model.NationalID,
                        PasswordHash = _passwordService.HashPassword(model.Password),
                        IsActive = true,
                        MustChangePassword = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.StudentLogins.Add(studentLogin);
                }

                await _context.SaveChangesAsync();

                // -- 3e. File operation (inside strategy so any failure triggers retry) --
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "student-photos");
                Directory.CreateDirectory(uploadsDir);
                var fileName = $"{student.ID}_{DateTime.Now:yyyyMMddHHmmssffff}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Photo!.CopyToAsync(stream);
                }
                savedFilePath = filePath;
                savedPhotoPath = $"/uploads/student-photos/{fileName}";

                // -- 3f. Update student photo path --
                student.Photo = savedPhotoPath;
                await _context.SaveChangesAsync();
            });

            #endregion

            #region === PHASE 4: POST-STRATEGY (audit, email — non-critical) ===

            string auditAction = isNewStudent
                ? "Application.NewStudentSubmitted"
                : "Application.ReturningStudentSubmitted";
            await _auditService.LogAsync(student!.ID, "Student", auditAction, "Application", application.ID);

            if (!string.IsNullOrEmpty(student.Email))
            {
                try
                {
                    string emailBody = $@"
                        <h3>تم استلام طلبك بنجاح</h3>
                        <p>رقم الطلب: <strong>{application.ID:00000}</strong></p>
                        <p>اسم المستخدم: <strong>{model.NationalID}</strong></p>
                        <p>سيتم مراجعة طلبك قريباً وسنخبرك بالنتيجة.</p>";
                    await _emailService.SendAsync(
                        student.Email,
                        "تأكيد تقديم طلب السكن - UniStay",
                        emailBody,
                        EmailType.ApplicationReceived,
                        student.ID);
                }
                catch (Exception ex)
                {
                    await _auditService.LogAsync(student.ID, "Student",
                        "Application.EmailFailed", "Application", application.ID,
                        null, new { Error = ex.Message });
                }
            }

            #endregion

            return RedirectToAction("Confirm", new { id = application.ID });
        }

        // GET: /Application/Confirm/{id}
        [HttpGet("Application/Confirm/{id}")]
        public async Task<IActionResult> Confirm(int id)
        {
            var app = await _context.Applications
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.ID == id);

            if (app == null) return NotFound();

            ViewBag.ApplicationNumber = app.ID.ToString("00000");
            ViewBag.NationalID = app.Student?.NationalID;
            ViewBag.StudentName = app.Student?.FullName;

            return View();
        }

        // GET: /Application/TrackStatus
        [HttpGet("Application/TrackStatus")]
        public IActionResult TrackStatus()
        {
            return View();
        }

        [HttpPost("Application/TrackStatus")]
        public async Task<IActionResult> TrackStatus(string nationalId, int applicationId)
        {
            var app = await _context.Applications
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.ID == applicationId && a.Student.NationalID == nationalId);

            if (app == null)
            {
                ViewBag.Error = "لم يتم العثور على الطلب. تأكد من البيانات المدخلة.";
                return View();
            }

            return View("TrackStatusResult", app);
        }
        private decimal? CalculateDistance(string governorate, string city)
        {
            var distances = new Dictionary<string, decimal>
            {
                { "أسيوط", 0m },          { "القاهرة", 375m },      { "الجيزة", 350m },
                { "الإسكندرية", 580m },    { "قنا", 190m },          { "الأقصر", 230m },
                { "أسوان", 320m },         { "سوهاج", 100m },        { "البحر الأحمر", 450m },
                { "الوادي الجديد", 700m }, { "مطروح", 800m },        { "الفيوم", 250m },
                { "المنيا", 150m },        { "بني سويف", 200m },     { "الشرقية", 500m },
                { "الدقهلية", 550m },      { "دمياط", 600m },        { "كفر الشيخ", 520m },
                { "الغربية", 480m },       { "المنوفية", 460m },     { "البحيرة", 500m },
                { "الإسماعيلية", 420m },   { "بورسعيد", 560m },      { "السويس", 400m },
                { "شمال سيناء", 550m },    { "جنوب سيناء", 600m }
            };
            return distances.TryGetValue(governorate, out var dist) ? dist : null;
        }

    }
}