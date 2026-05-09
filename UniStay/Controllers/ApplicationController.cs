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

        // POST: /Application/Apply
        [HttpPost("Application/Apply")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplicationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // FIX 1: التحقق من تطابق كلمتي المرور (احتياطي إضافي فوق [Compare])
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "كلمتا المرور غير متطابقتين");
                return View(model);
            }

            // 1. إنشاء أو تحديث Student
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.NationalID == model.NationalID);

            bool isNewStudent = student == null;

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

            // FIX 2: التحقق من وجود طلب مسبق لنفس العام الدراسي (تفادي UNIQUE constraint violation)
            string currentAcademicYear = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}";
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

            // 2. حفظ Guardian (Father + ولي الأمر عند الحاجة)
            var father = new Guardian
            {
                StudentID = student.ID,
                GuardianType = "Father",
                FullName = model.FatherName,
                Job = model.FatherJob,
                Address = model.FatherAddress,
                IsDeceased = model.IsFatherDeceased
            };
            _context.Guardians.Add(father);

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

            await _context.SaveChangesAsync();

            // FIX 3: تحديد DormitoryCityID بناءً على جنس الطالب بدلاً من القيمة الثابتة 1
            // يجب اختيار المدينة المناسبة من قاعدة البيانات بناءً على gender
            var dormitoryCity = await _context.DormitoryCities
                .Where(d => d.IsActive == true && d.IsDeleted == false
                    && (d.CityType == (model.Gender == "Male" ? "Male" : "Female")
                        || d.CityType == "Mixed"))
                .FirstOrDefaultAsync();

            if (dormitoryCity == null)
            {
                ModelState.AddModelError("", "لا توجد مدينة جامعية متاحة حالياً. يرجى المحاولة لاحقاً أو التواصل مع الإدارة.");
                return View(model);
            }

            // 3. إنشاء Application
            var application = new Application
            {
                StudentID = student.ID,
                DormitoryCityID = dormitoryCity.ID,
                AcademicYear = currentAcademicYear,
                StudentType = model.IsReturningStudent ? "Returning" : "New",
                HousingType = model.HousingType,
                HasSpecialNeeds = model.HasMedicalCondition,
                SpecialNeedsDescription = model.MedicalDescription,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            // FIX 4: التحقق من عدم وجود حساب مسبق قبل إنشاء StudentLogin (تفادي UNIQUE constraint violation)
            var existingLogin = await _context.StudentLogins
                .FirstOrDefaultAsync(l => l.StudentID == student.ID);

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
                await _context.SaveChangesAsync();
            }

            // 5. تسجيل في Audit — FIX 5: استخدام isNewStudent في الـ action
            string auditAction = isNewStudent ? "Application.NewStudentSubmitted" : "Application.ReturningStudentSubmitted";
            await _auditService.LogAsync(student.ID, "Student", auditAction, "Application", application.ID);

            // 6. إرسال إيميل تأكيد
            if (!string.IsNullOrEmpty(student.Email))
            {
                string emailBody = $@"
                    <h3>تم استلام طلبك بنجاح</h3>
                    <p>رقم الطلب: <strong>{application.ID:00000}</strong></p>
                    <p>اسم المستخدم: <strong>{model.NationalID}</strong></p>
                    <p>سيتم مراجعة طلبك قريباً وسنخطرك بالنتيجة.</p>";

                await _emailService.SendAsync(
                    student.Email,
                    "تأكيد تقديم طلب السكن - UniStay",
                    emailBody,
                    EmailType.ApplicationReceived,
                    student.ID);
            }

            // 7. التوجيه إلى صفحة التأكيد
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
    }
}