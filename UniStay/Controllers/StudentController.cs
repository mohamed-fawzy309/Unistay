using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Application;
using UniStay.ViewModels.Attendance;

namespace UniStay.Controllers
{
    [TypeFilter(typeof(StudentAuthFilter))]
    public class StudentController : Controller
    {
        private readonly AssuitDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;

        public StudentController(AssuitDbContext context, IAuditService auditService, IEmailService emailService)
        {
            _context = context;
            _auditService = auditService;
            _emailService = emailService;
        }

        // GET: /Student/Home
        [HttpGet]
        public async Task<IActionResult> Home()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var student = await _context.Students.FindAsync(studentId.Value);

            var latestApp = await _context.Applications
                .Where(a => a.StudentID == studentId.Value)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            ViewBag.StudentName = student?.FullName ?? "طالب";
            ViewBag.LatestStatus = latestApp?.Status ?? "لا يوجد طلبات حالية";
            ViewBag.LatestStatusColor = GetStatusColor(latestApp?.Status);

            return View();
        }

        // GET: /Student/ApplicationSchedule
        [HttpGet]
        public async Task<IActionResult> ApplicationSchedule()
        {
            var currentYear = DateTime.Now.Year.ToString();
            var schedules = await _context.ApplicationSchedules
                .Include(s => s.DormitoryCity)
                .Where(s => s.AcademicYear.Contains(currentYear))
                .OrderBy(s => s.NewStudentsOpenDate)
                .ToListAsync();

            return View(schedules);
        }

        // GET: /Student/Instructions
        // ✅ FIX: استبدلنا .Include(i => i.Attachments) بـ .Include(i => i.HousingInstructionAttachments)
        [HttpGet]
        public async Task<IActionResult> Instructions()
        {
            var instructions = await _context.HousingInstructions
                .Include(i => i.HousingInstructionAttachments)
                .Where(i => i.IsActive == true)
                .OrderBy(i => i.SortOrder)
                .ToListAsync();

            return View(instructions);
        }

        // GET: /Student/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var student = await _context.Students.FindAsync(studentId.Value);
            if (student == null) return NotFound();

            var app = await _context.Applications
                .Where(a => a.StudentID == studentId.Value)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (app != null && (app.Status == "Accepted" || app.Status == "Allocated"))
            {
                TempData["Warning"] = "لا يمكن تعديل البيانات بعد القبول النهائي. يرجى التواصل مع الإدارة.";
                return RedirectToAction("StatusReport");
            }

            var model = new ApplicationViewModel
            {
                NationalID = student.NationalID,
                FullName = student.FullName,
                BirthDate = student.BirthDate,
                Gender = student.Gender,
                Religion = student.Religion,
                Nationality = student.Nationality,
                Governorate = student.Governorate,
                Markaz = student.Markaz,
                City = student.City,
                Address = student.Address,
                Email = student.Email,
                Phone = student.Phone,
                Faculty = student.Faculty,
                AcademicYear = student.AcademicYear,
                GradePercentage = student.GradePercentage.GetValueOrDefault(0),
                DistanceFromUniv = student.DistanceFromUniv,
                HasFamilyAbroad = student.HasFamilyAbroad.GetValueOrDefault(false),
                HasMedicalCondition = student.HasMedicalCondition.GetValueOrDefault(false),
                MedicalDescription = student.MedicalDescription
            };

            return View(model);
        }

        // POST: /Student/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ApplicationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var student = await _context.Students.FindAsync(studentId.Value);
            if (student == null) return NotFound();

            student.FullName = model.FullName;
            student.Phone = model.Phone;
            student.Email = model.Email;
            student.Address = model.Address;
            student.City = model.City;
            student.Markaz = model.Markaz;
            student.Governorate = model.Governorate;
            student.Faculty = model.Faculty;
            student.AcademicYear = (byte?)model.AcademicYear;
            student.GradePercentage = model.GradePercentage;
            student.DistanceFromUniv = model.DistanceFromUniv;
            student.HasFamilyAbroad = model.HasFamilyAbroad;
            student.HasMedicalCondition = model.HasMedicalCondition;
            student.MedicalDescription = model.MedicalDescription;
            student.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(studentId.Value, "Student", "Profile.Updated", "Student", studentId.Value);

            TempData["Success"] = "تم تحديث بياناتك بنجاح";
            return RedirectToAction("EditProfile");
        }

        // GET: /Student/StatusReport
        [HttpGet]
        public async Task<IActionResult> StatusReport()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var student = await _context.Students
                .Include(s => s.Guardians)
                .FirstOrDefaultAsync(s => s.ID == studentId.Value);

            var allocation = await _context.Allocations
                .Include(a => a.CityRoom)
                    .ThenInclude(r => r.CityBuilding)
                        .ThenInclude(b => b.DormitoryCity)
                .Where(a => a.StudentID == studentId.Value && a.Status == "Active")
                .FirstOrDefaultAsync();

            var absences = await _context.Absences
                .Where(a => a.StudentID == studentId.Value)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.Allocation = allocation;
            ViewBag.Absences = absences;

            return View(student);
        }

        // GET: /Student/ApplicationStatus
        [HttpGet]
        public async Task<IActionResult> ApplicationStatus()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var applications = await _context.Applications
                .Where(a => a.StudentID == studentId.Value)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var activeAlloc = await _context.Allocations
                .FirstOrDefaultAsync(a => a.StudentID == studentId && a.Status == "Active");
            ViewBag.HasActiveAllocation = activeAlloc != null;

            return View(applications);
        }

        // GET: /Student/SelectRoom
        [HttpGet]
        public async Task<IActionResult> SelectRoom()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var app = await _context.Applications
                .Include(a => a.DormitoryCity)
                .FirstOrDefaultAsync(a => a.StudentID == studentId && a.Status == "Accepted");
            if (app == null)
            {
                TempData["Error"] = "ليس لديك طلب مقبول لحجز غرفة";
                return RedirectToAction("Home");
            }

            var buildings = await _context.CityBuildings
                .Where(b => b.DormitoryCityID == app.DormitoryCityID && b.IsActive && b.IsDeleted != true)
                .Include(b => b.CityRooms.Where(r => r.IsActive == true && r.IsDeleted != true
                    && r.CurrentOccupancy < r.BedsCount
                    && r.RoomType != "إشراف" && r.RoomType != "مخزن"))
                .OrderBy(b => b.BuildingName)
                .ToListAsync();

            var reserved = await _context.Allocations
                .Where(a => a.Status == "Reserved" || a.Status == "Active")
                .Select(a => new { a.CityRoomID, a.BedNumber })
                .ToListAsync();
            ViewBag.ReservedBeds = reserved.Select(r => $"{r.CityRoomID}-{r.BedNumber}").ToHashSet();
            ViewBag.CityName = app.DormitoryCity.Name;
            ViewBag.ApplicationID = app.ID;
            ViewBag.AcademicYear = app.AcademicYear;
            return View(buildings);
        }

        // POST: /Student/ReserveRoom
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ReserveRoom(int roomId, byte bedNumber)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return Json(new { success = false, message = "غير مصرح" });

            var room = await _context.CityRooms
                .Include(r => r.CityBuilding)
                .FirstOrDefaultAsync(r => r.ID == roomId);
            if (room == null || room.CurrentOccupancy >= room.BedsCount)
                return Json(new { success = false, message = "الغرفة ممتلئة" });

            var bedTaken = await _context.Allocations.AnyAsync(a =>
                a.CityRoomID == roomId && a.BedNumber == bedNumber
                && (a.Status == "Active" || a.Status == "Reserved"));
            if (bedTaken)
                return Json(new { success = false, message = "هذا السرير مشغول بالفعل" });

            var app = await _context.Applications
                .FirstOrDefaultAsync(a => a.StudentID == studentId && a.Status == "Accepted");
            if (app == null)
                return Json(new { success = false, message = "لا يوجد طلب مقبول" });

            var existing = await _context.Allocations
                .FirstOrDefaultAsync(a => a.StudentID == studentId && a.Status == "Reserved");
            if (existing != null)
                return Json(new { success = false, message = "لديك حجز مؤقت بالفعل" });

            var alloc = new Allocation
            {
                ApplicationID = app.ID,
                StudentID = studentId.Value,
                CityRoomID = roomId,
                BedNumber = bedNumber,
                AcademicYear = app.AcademicYear,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                Status = "Reserved",
                AllocatedAt = DateTime.UtcNow,
            };

            var payment = new Payment
            {
                StudentID = studentId.Value,
                ApplicationID = app.ID,
                Allocation = alloc,
                PaymentType = "Housing",
                Amount = 1000,
                PaidAmount = 0,
                Status = "Pending",
                AcademicYear = app.AcademicYear,
                RecordedAt = DateTime.UtcNow,
            };
            _context.Allocations.Add(alloc);
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم حجز الغرفة. لديك 24 ساعة للدفع." });
        }

        // GET: /Student/Payment
        [HttpGet]
        public async Task<IActionResult> Payment()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var alloc = await _context.Allocations
                .Include(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
                .FirstOrDefaultAsync(a => a.StudentID == studentId && a.Status == "Reserved");
            if (alloc == null)
            {
                TempData["Error"] = "لا يوجد حجز مؤقت";
                return RedirectToAction("Home");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.AllocationID == alloc.ID && p.Status == "Pending");

            var deadline = alloc.AllocatedAt?.AddHours(24) ?? DateTime.UtcNow.AddHours(24);
            deadline = DateTime.SpecifyKind(deadline, DateTimeKind.Utc);
            var isExpired = DateTime.UtcNow > deadline;

            if (isExpired)
            {
                await CancelReservationInternal(alloc, payment);
                TempData["Error"] = "انتهت مهلة الـ 24 ساعة. تم إلغاء الحجز.";
                return RedirectToAction("Home");
            }

            ViewBag.Deadline = deadline;
            ViewBag.RemainingSeconds = (int)(deadline - DateTime.UtcNow).TotalSeconds;
            ViewBag.Amount = payment?.Amount ?? 1000;
            Console.WriteLine($"AllocatedAt = {alloc.AllocatedAt}");
            Console.WriteLine($"UtcNow      = {DateTime.UtcNow}");
            Console.WriteLine($"Now         = {DateTime.Now}");
            Console.WriteLine($"Deadline    = {deadline}");
            return View(alloc);
        }

        // POST: /Student/ProcessPayment
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ProcessPayment()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return Json(new { success = false, message = "غير مصرح" });

            var alloc = await _context.Allocations
                .Include(a => a.CityRoom)
                .FirstOrDefaultAsync(a => a.StudentID == studentId && a.Status == "Reserved");
            if (alloc == null)
                return Json(new { success = false, message = "لا يوجد حجز مؤقت" });

            var deadline = alloc.AllocatedAt.HasValue
                ? new DateTime(alloc.AllocatedAt.Value.Ticks + TimeSpan.TicksPerHour * 24, DateTimeKind.Utc)
                : DateTime.UtcNow;
            if (DateTime.UtcNow > deadline)
            {
                await CancelReservationInternal(alloc, null);
                return Json(new { success = false, message = "انتهت مهلة الـ 24 ساعة. تم إلغاء الحجز.", expired = true });
            }

            alloc.Status = "Active";

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.AllocationID == alloc.ID && p.Status == "Pending");
            if (payment != null)
            {
                payment.Status = "Completed";
                payment.PaidAmount = payment.Amount;
                payment.ReceiptNumber = $"SIM-{DateTime.Now:yyyyMMdd}-{DateTime.Now.Ticks % 100000}";
                payment.PaymentMethod = "Simulation";
                payment.RecordedBy = null;
            }

            if (alloc.CityRoom != null)
                alloc.CityRoom.CurrentOccupancy = (byte)(alloc.CityRoom.CurrentOccupancy + 1);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم الدفع بنجاح! تم تأكيد تسكينك." });
        }

        private async Task CancelReservationInternal(Allocation alloc, Payment? payment)
        {
            alloc.Status = "Cancelled";

            if (payment != null)
                payment.Status = "Overdue";

            await _context.SaveChangesAsync();
        }

        // GET: /Student/RequestAbsence
        [HttpGet]
        public IActionResult RequestAbsence()
        {
            return View();
        }

        // POST: /Student/RequestAbsence
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAbsence(Absence model)
        {
            if (!ModelState.IsValid) return View(model);

            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var student = await _context.Students.FindAsync(studentId.Value);
            if (student == null) return NotFound();

            model.StudentID = studentId.Value;
            model.Status = "Pending";
            model.RequestedBy = "Student";
            model.CreatedAt = DateTime.UtcNow;

            var currentAllocation = await _context.Allocations
                .FirstOrDefaultAsync(a => a.StudentID == studentId.Value && a.Status == "Active");

            if (currentAllocation != null)
            {
                var room = await _context.CityRooms
                    .Include(r => r.CityBuilding)
                    .FirstOrDefaultAsync(r => r.ID == currentAllocation.CityRoomID);

                if (room?.CityBuilding != null)
                {
                    model.DormitoryCityID = room.CityBuilding.DormitoryCityID;
                }
            }

            _context.Absences.Add(model);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(student.Email))
            {
                await _emailService.SendAsync(
                    student.Email,
                    "تم استلام طلب الإذن/الغياب",
                    $"تم استلام طلبك رقم #{model.ID} وسيتم مراجعته قريباً.",
                    EmailType.General,
                    studentId.Value);
            }

            TempData["Success"] = "تم إرسال الطلب بنجاح";
            return RedirectToAction("RequestAbsence");
        }

        // GET: /Student/Payments
        [HttpGet]
        public async Task<IActionResult> Payments()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var alloc = await _context.Allocations
                .Include(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
                .FirstOrDefaultAsync(a => a.StudentID == studentId && a.Status == "Active");
            if (alloc == null)
            {
                TempData["Error"] = "أنت غير مسكن حالياً";
                return RedirectToAction("Home");
            }

            await EnsureMonthlyFees(alloc);

            var payments = await _context.Payments
                .Where(p => p.StudentID == studentId && p.AcademicYear == alloc.AcademicYear)
                .OrderByDescending(p => p.RecordedAt)
                .ToListAsync();

            var violations = await _context.Violations
                .Where(v => v.StudentID == studentId && v.FineAmount.HasValue)
                .OrderByDescending(v => v.RecordedAt)
                .ToListAsync();

            var totalDue = payments.Where(p => p.Status != "Completed").Sum(p => p.Amount);
            var totalPaid = payments.Where(p => p.Status == "Completed").Sum(p => p.PaidAmount);
            var monthlyFee = 500m;

            ViewBag.Allocation = alloc;
            ViewBag.TotalDue = totalDue + violations.Where(v => v.Status == "Active" && v.FineAmount.HasValue).Sum(v => v.FineAmount!.Value);
            ViewBag.TotalPaid = totalPaid + violations.Where(v => v.FinePaid.HasValue).Sum(v => v.FinePaid!.Value);
            ViewBag.MonthlyFee = monthlyFee;
            ViewBag.Violations = violations;

            var months = new[] { "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر", "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس" };
            var now = DateTime.UtcNow;
            ViewBag.CurrentMonthLabel = now.Day >= 20 ? months[now.Month - 1] : null;

            return View(payments);
        }

        // POST: /Student/PayItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayItem(int paymentId)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return Json(new { success = false, message = "غير مصرح" });

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ID == paymentId && p.StudentID == studentId);
            if (payment == null)
                return Json(new { success = false, message = "الدفعة غير موجودة" });

            payment.Status = "Completed";
            payment.PaidAmount = payment.Amount;
            payment.PaymentMethod = "StudentPortal";
            payment.ReceiptNumber = $"SIM-{DateTime.Now:yyyyMMdd}-{DateTime.Now.Ticks % 100000}";
            payment.RecordedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "تم الدفع بنجاح" });
        }

        // POST: /Student/PayViolationFine
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayViolationFine(int violationId)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return Json(new { success = false, message = "غير مصرح" });

            var violation = await _context.Violations
                .FirstOrDefaultAsync(v => v.ID == violationId && v.StudentID == studentId);
            if (violation == null)
                return Json(new { success = false, message = "المخالفة غير موجودة" });

            if (violation.Status == "Paid")
                return Json(new { success = false, message = "تم دفع الغرامة مسبقاً" });

            violation.FinePaid = violation.FineAmount;
            violation.Status = "Paid";
            violation.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "تم دفع الغرامة بنجاح" });
        }

        // GET: /Student/Violations
        [HttpGet]
        public async Task<IActionResult> Violations()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var violations = await _context.Violations
                .Where(v => v.StudentID == studentId)
                .Include(v => v.DormitoryCity)
                .OrderByDescending(v => v.RecordedAt)
                .ToListAsync();

            var totalFines = violations.Where(v => v.FineAmount.HasValue).Sum(v => v.FineAmount!.Value);
            var totalPaid = violations.Where(v => v.FinePaid.HasValue).Sum(v => v.FinePaid!.Value);

            ViewBag.TotalFines = totalFines;
            ViewBag.TotalPaid = totalPaid;

            return View(violations);
        }

        // GET: /Student/Meals
        [HttpGet]
        public async Task<IActionResult> Meals()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var alloc = await _context.Allocations
                .Include(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
                .FirstOrDefaultAsync(a => a.StudentID == studentId && a.Status == "Active");
            if (alloc == null) return RedirectToAction("Home");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var meals = await _context.Meals
                .Where(m => m.StudentID == studentId && m.MealDate >= today && m.MealDate <= today.AddDays(6))
                .OrderBy(m => m.MealDate).ThenBy(m => m.MealType)
                .ToListAsync();

            return View(meals);
        }

        // POST: /Student/ToggleMealBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMealBooking(int mealId)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return Json(new { success = false, message = "غير مصرح" });

            var meal = await _context.Meals
                .FirstOrDefaultAsync(m => m.ID == mealId && m.StudentID == studentId);
            if (meal == null)
                return Json(new { success = false, message = "الوجبة غير موجودة" });

            if (meal.MealDate <= DateOnly.FromDateTime(DateTime.UtcNow))
                return Json(new { success = false, message = "لا يمكن تعديل حجز وجبة ماضية" });

            meal.IsBooked = !(meal.IsBooked ?? true);
            meal.IsActive = meal.IsBooked;
            await _context.SaveChangesAsync();

            return Json(new { success = true, booked = meal.IsBooked });
        }

        // GET: /Student/AnnouncementsList
        [HttpGet]
        public async Task<IActionResult> AnnouncementsList()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var announcements = await _context.Announcements
                .Where(a => a.IsPublished == true && (!a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow))
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            var absences = await _context.Absences
                .Where(a => a.StudentID == studentId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.Absences = absences;
            return View(announcements);
        }

        // GET: /Student/Attendance
        [HttpGet]
        public async Task<IActionResult> AttendanceHistory()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null) return RedirectToAction("Login", "StudentAccount");

            var now = DateTime.Now;
            var today = now.Date;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var thirtyDaysAgo = now.AddDays(-30).Date;

            var todayLog = await _context.AttendanceLogs
                .Where(l => l.StudentID == studentId.Value
                    && l.RecognizedAt.HasValue
                    && l.RecognizedAt.Value.Date == today)
                .OrderByDescending(l => l.RecognizedAt)
                .FirstOrDefaultAsync();

            var presentDaysThisMonth = await _context.AttendanceLogs
                .Where(l => l.StudentID == studentId.Value
                    && l.RecognizedAt.HasValue
                    && l.RecognizedAt.Value.Date >= startOfMonth
                    && l.RecognizedAt.Value.Date <= today)
                .Select(l => l.RecognizedAt!.Value.Date)
                .Distinct()
                .CountAsync();

            var totalSessionDaysThisMonth = await _context.AttendanceSessions
                .Where(s => s.StartedAt.HasValue
                    && s.StartedAt.Value.Date >= startOfMonth
                    && s.StartedAt.Value.Date <= today)
                .Select(s => s.StartedAt!.Value.Date)
                .Distinct()
                .CountAsync();

            var logs30 = await _context.AttendanceLogs
                .Where(l => l.StudentID == studentId.Value
                    && l.RecognizedAt.HasValue
                    && l.RecognizedAt.Value.Date >= thirtyDaysAgo)
                .Select(l => new { l.RecognizedAt!.Value.Date, l.RecognizedAt!.Value })
                .ToListAsync();

            var logMap = logs30
                .GroupBy(x => x.Date)
                .ToDictionary(g => g.Key, g => g.First().Value);

            var historyItems = new List<AttendanceHistoryItemViewModel>();
            for (var date = thirtyDaysAgo; date <= today; date = date.AddDays(1))
            {
                var hasLog = logMap.TryGetValue(date, out var time);
                historyItems.Add(new AttendanceHistoryItemViewModel
                {
                    Date = date,
                    Status = hasLog ? "حاضر" : "غائب",
                    RecognitionTime = hasLog ? time : null
                });
            }

            var vm = new StudentAttendanceHistoryViewModel
            {
                IsPresentToday = todayLog != null,
                TodayRecognitionTime = todayLog?.RecognizedAt,
                PresentDaysThisMonth = presentDaysThisMonth,
                TotalSessionDaysThisMonth = totalSessionDaysThisMonth,
                AttendancePercentage = totalSessionDaysThisMonth > 0
                    ? Math.Round((decimal)presentDaysThisMonth / totalSessionDaysThisMonth * 100, 1)
                    : 0,
                HistoryItems = historyItems.OrderByDescending(h => h.Date).ToList()
            };

            return View(vm);
        }

        private async Task EnsureMonthlyFees(Allocation alloc)
        {
            var now = DateTime.UtcNow;
            var monthlyFee = 500m;

            // Only show monthly fee after the 20th of the month
            if (now.Day < 20) return;

            var months = new[] { "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر", "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس" };
            var currentMonthLabel = months[now.Month - 1];

            var exists = await _context.Payments
                .AnyAsync(p => p.AllocationID == alloc.ID && p.PaymentType == "MonthlyFee" && p.Notes == currentMonthLabel);

            if (exists) return;

            var dueDate = new DateTime(now.Year, now.Month, 1);
            _context.Payments.Add(new Payment
            {
                StudentID = alloc.StudentID,
                ApplicationID = alloc.ApplicationID,
                AllocationID = alloc.ID,
                PaymentType = "MonthlyFee",
                Amount = monthlyFee,
                PaidAmount = 0,
                Status = "Pending",
                AcademicYear = alloc.AcademicYear,
                Notes = currentMonthLabel,
                RecordedAt = dueDate
            });

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private int? GetCurrentStudentId()
        {
            var claim = User.FindFirstValue("StudentID");
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var studentId))
                return null;
            return studentId;
        }

        private string GetStatusColor(string? status)
        {
            return status switch
            {
                "Pending" => "secondary",
                "UnderReview" => "primary",
                "Accepted" => "success",
                "Rejected" => "danger",
                "Allocated" => "dark",
                _ => "light"
            };
        }
    }
}