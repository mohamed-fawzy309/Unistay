using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Attendance;

namespace UniStay.Controllers
{
    [Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
    public class AttendanceController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;

        public AttendanceController(AssuitDbContext db, IAuditService audit, IEmailService email)
        {
            _db = db;
            _audit = audit;
            _email = email;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordAbsence([FromBody] RecordAbsenceViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صحيحة" });

            var student = await _db.Students.FindAsync(model.StudentID);
            if (student == null)
                return Json(new { success = false, message = "الطالب غير موجود" });

            var cityId = await _db.Allocations
                .Where(a => a.StudentID == model.StudentID && a.Status == "Active")
                .Select(a => a.CityRoom!.CityBuilding.DormitoryCityID)
                .FirstOrDefaultAsync();

            var absence = new Absence
            {
                StudentID = model.StudentID,
                DormitoryCityID = cityId,
                AbsenceDate = model.AbsenceDate,
                AbsenceType = "Absence",
                Status = "Approved",
                RequestedBy = "Staff",
                Reason = model.Reason,
                CreatedAt = DateTime.UtcNow
            };

            _db.Absences.Add(absence);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Attendance.RecordAbsence",
                "Absence", absence.ID, null, new { model.StudentID, model.AbsenceDate, model.Reason });

            return Json(new { success = true, message = "تم تسجيل الغياب بنجاح" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPermission([FromBody] RequestPermissionViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صحيحة" });

            var student = await _db.Students.FindAsync(model.StudentID);
            if (student == null)
                return Json(new { success = false, message = "الطالب غير موجود" });

            var cityId = await _db.Allocations
                .Where(a => a.StudentID == model.StudentID && a.Status == "Active")
                .Select(a => a.CityRoom!.CityBuilding.DormitoryCityID)
                .FirstOrDefaultAsync();

            var absence = new Absence
            {
                StudentID = model.StudentID,
                DormitoryCityID = cityId,
                AbsenceDate = model.FromDate,
                ToDate = model.ToDate,
                AbsenceType = "Permission",
                Status = "Pending",
                RequestedBy = "Guardian",
                GuardianName = model.GuardianName,
                GuardianRelation = model.GuardianRelation,
                GuardianPhone = model.GuardianPhone,
                Reason = model.Reason,
                CreatedAt = DateTime.UtcNow
            };

            _db.Absences.Add(absence);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Attendance.RequestPermission",
                "Absence", absence.ID, null, new { model.StudentID, model.FromDate, model.ToDate, model.GuardianName });

            return Json(new { success = true, message = "تم تسجيل طلب الإذن بنجاح" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, [FromBody] ApprovePermissionViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صحيحة" });

            var absence = await _db.Absences
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.ID == id);

            if (absence == null)
                return Json(new { success = false, message = "الغياب غير موجود" });

            if (absence.AbsenceType != "Permission")
                return Json(new { success = false, message = "هذا الإجراء مخصص للإذن فقط" });

            var oldStatus = absence.Status;

            absence.Status = model.Status;
            absence.ReviewedBy = CurrentUserId;
            absence.ReviewedAt = DateTime.UtcNow;

            if (model.Status == "Rejected" && !string.IsNullOrEmpty(model.RejectionReason))
                absence.Reason = model.RejectionReason;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Attendance.ApprovePermission",
                "Absence", id, new { Status = oldStatus }, new { Status = model.Status });

            if (absence.Student != null && !string.IsNullOrEmpty(absence.Student.Email))
            {
                string subject = model.Status == "Approved"
                    ? "تم الموافقة على طلب الإذن - UniStay"
                    : "تم رفض طلب الإذن - UniStay";

                string body = model.Status == "Approved"
                    ? $"<h3>تم الموافقة</h3><p>عزيزي {absence.Student.FullName}، تم الموافقة على طلب الإذن الخاص بك.</p>"
                    : $"<h3>نأسف</h3><p>عزيزي {absence.Student.FullName}، تم رفض طلب الإذن الخاص بك.</p>"
                    + (!string.IsNullOrEmpty(model.RejectionReason) ? $"<p>السبب: {model.RejectionReason}</p>" : "");

                var emailType = model.Status == "Approved" ? EmailType.AbsenceApproved : EmailType.AbsenceRejected;
                await _email.SendAsync(absence.Student.Email, subject, body, emailType, absence.Student.ID);
            }

            return Json(new { success = true, message = model.Status == "Approved" ? "تم الموافقة على الإذن" : "تم رفض الإذن" });
        }

        [HttpGet]
        [RequirePermission("Attendance.Manage", "CanView")]
        public async Task<IActionResult> Report(
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            int? dormitoryCityId = null,
            int? studentId = null,
            int page = 1)
        {
            var query = _db.Absences
                .Include(a => a.Student)
                .Include(a => a.ReviewedByNavigation)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(a => a.AbsenceDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.AbsenceDate <= toDate.Value);

            if (dormitoryCityId.HasValue)
                query = query.Where(a => a.DormitoryCityID == dormitoryCityId.Value);

            if (studentId.HasValue)
                query = query.Where(a => a.StudentID == studentId.Value);

            var total = await query.CountAsync();

            var records = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * 30)
                .Take(30)
                .Select(a => new AttendanceRowViewModel
                {
                    ID = a.ID,
                    StudentName = a.Student.FullName,
                    NationalID = a.Student.NationalID,
                    AbsenceDate = a.AbsenceDate,
                    ToDate = a.ToDate,
                    AbsenceType = a.AbsenceType,
                    Status = a.Status,
                    GuardianName = a.GuardianName,
                    Reason = a.Reason,
                    CreatedAt = a.CreatedAt,
                    ReviewedByName = a.ReviewedByNavigation != null ? a.ReviewedByNavigation.Name : null
                })
                .ToListAsync();

            var vm = new AttendanceReportViewModel
            {
                Records = records,
                FromDate = fromDate,
                ToDate = toDate,
                DormitoryCityID = dormitoryCityId,
                StudentID = studentId,
                Page = page,
                TotalPages = (int)Math.Ceiling(total / 30.0),
                Cities = await _db.DormitoryCities
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .Select(c => new CityLookup { ID = c.ID, Name = c.Name })
                    .ToListAsync()
            };

            return View(vm);
        }
    }
}
