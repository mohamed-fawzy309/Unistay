using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Payment;

namespace UniStay.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class PaymentController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;
        private readonly IReportExportService _export;

        public PaymentController(AssuitDbContext db, IAuditService audit, IEmailService email, IReportExportService export)
        {
            _db = db;
            _audit = audit;
            _email = email;
            _export = export;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        [HttpGet]
        public async Task<IActionResult> StudentPayments(int studentId)
        {
            var student = await _db.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            var payments = await _db.Payments
                .Where(p => p.StudentID == studentId)
                .Include(p => p.RecordedByNavigation)
                .OrderByDescending(p => p.RecordedAt)
                .Select(p => new PaymentRowViewModel
                {
                    ID = p.ID,
                    PaymentType = p.PaymentType,
                    Amount = p.Amount,
                    PaidAmount = p.PaidAmount,
                    Status = p.Status,
                    PaymentMethod = p.PaymentMethod,
                    ReceiptNumber = p.ReceiptNumber,
                    RecordedAt = p.RecordedAt,
                    RecordedByName = p.RecordedByNavigation!.Name,
                    Notes = p.Notes
                })
                .ToListAsync();

            var totalPaid = payments.Where(p => p.Status == "Completed").Sum(p => p.PaidAmount);
            var totalDue = payments.Where(p => p.Status != "Completed").Sum(p => p.Amount);

            return View(new StudentPaymentsViewModel
            {
                StudentID = studentId,
                StudentName = student.FullName,
                NationalID = student.NationalID,
                Faculty = student.Faculty,
                TotalPaid = totalPaid,
                TotalDue = totalDue,
                Payments = payments
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Record(RecordPaymentViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "بيانات غير صالحة" });

            var receiptNumber = $"RCP-{DateTime.Now:yyyyMMdd}-{DateTime.Now.Ticks % 100000}";

            var payment = new Payment
            {
                StudentID = model.StudentID,
                ApplicationID = model.ApplicationID,
                AllocationID = model.AllocationID,
                PaymentType = model.PaymentType,
                Amount = model.Amount,
                PaidAmount = model.PaidAmount,
                Status = model.PaidAmount >= model.Amount ? "Completed" : "Partial",
                PaymentMethod = model.PaymentMethod,
                ReceiptNumber = receiptNumber,
                AcademicYear = model.AcademicYear,
                RecordedBy = CurrentUserId,
                RecordedAt = DateTime.UtcNow,
                Notes = model.Notes
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Payment.Record", "Payment",
                payment.ID, null, new { model.StudentID, model.PaymentType, model.Amount, model.PaidAmount });

            return Json(new { success = true, message = "تم تسجيل الدفعة", receiptNumber });
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            var payment = await _db.Payments
                .Include(p => p.Student)
                .Include(p => p.RecordedByNavigation)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (payment == null) return NotFound();

            return View(new ReceiptViewModel
            {
                PaymentID = payment.ID,
                ReceiptNumber = payment.ReceiptNumber ?? "",
                StudentName = payment.Student?.FullName ?? "",
                NationalID = payment.Student?.NationalID ?? "",
                PaymentType = payment.PaymentType,
                Amount = payment.Amount,
                PaidAmount = payment.PaidAmount,
                PaymentMethod = payment.PaymentMethod ?? "",
                RecordedAt = payment.RecordedAt,
                RecordedByName = payment.RecordedByNavigation?.Name,
                Notes = payment.Notes,
                AcademicYear = payment.AcademicYear ?? ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkOverdue(int paymentId)
        {
            var payment = await _db.Payments.FindAsync(paymentId);
            if (payment == null) return Json(new { success = false, message = "الدفعة غير موجودة" });

            var oldStatus = payment.Status;
            payment.Status = "Overdue";

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Payment.MarkOverdue", "Payment",
                paymentId, new { Status = oldStatus }, new { Status = "Overdue" });

            var student = await _db.Students.FindAsync(payment.StudentID);
            if (student?.Email != null)
            {
                await _email.SendAsync(student.Email, "تذكير بدفع مستحقات - UniStay",
                    $"عزيزي {student.FullName}، لديك دفعة متأخرة بقيمة {payment.Amount}",
                    Services.Interfaces.EmailType.PaymentReminder, student.ID);
            }

            return Json(new { success = true, message = "تم وضع الدفعة كمتأخرة" });
        }

        [HttpGet]
        public async Task<IActionResult> GatewayLog(string? status, int page = 1)
        {
            var query = _db.PaymentGatewayLogs
                .Include(g => g.Student)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(g => g.Status == status);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / 20.0);

            var logs = await query
                .OrderByDescending(g => g.CreatedAt)
                .Skip((page - 1) * 20)
                .Take(20)
                .Select(g => new GatewayLogRowViewModel
                {
                    ID = g.ID,
                    TransactionID = g.TransactionID,
                    StudentName = g.Student!.FullName,
                    GatewayType = g.GatewayType,
                    Amount = g.Amount,
                    Status = g.Status,
                    CreatedAt = g.CreatedAt
                })
                .ToListAsync();

            return View(new PaymentGatewayLogViewModel
            {
                Logs = logs,
                FilterStatus = status,
                Page = page,
                TotalPages = totalPages
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOnline(int logId)
        {
            var log = await _db.PaymentGatewayLogs
                .Include(g => g.Payment)
                .FirstOrDefaultAsync(g => g.ID == logId);

            if (log == null || log.Payment == null)
                return Json(new { success = false, message = "السجل غير موجود" });

            log.Status = "Confirmed";
            log.Payment.Status = "Completed";

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Payment.ConfirmOnline", "Payment",
                log.PaymentID, null, new { LogId = logId, Status = "Confirmed" });

            return Json(new { success = true, message = "تم تأكيد الدفعة" });
        }

        [HttpGet]
        public async Task<IActionResult> Report(DateTime? fromDate, DateTime? toDate, int? cityId)
        {
            var paymentsQuery = _db.Payments.AsQueryable();

            if (fromDate.HasValue) paymentsQuery = paymentsQuery.Where(p => p.RecordedAt >= fromDate.Value);
            if (toDate.HasValue) paymentsQuery = paymentsQuery.Where(p => p.RecordedAt <= toDate.Value.AddDays(1));

            if (cityId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p =>
                    p.Student!.Applications!.Any(a => a.DormitoryCityID == cityId));
            }

            var allPayments = await paymentsQuery.ToListAsync();

            var summary = allPayments
                .GroupBy(p => p.PaymentType)
                .Select(g => new PaymentSummaryRowViewModel
                {
                    PaymentType = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount),
                    TotalPaid = g.Sum(p => p.PaidAmount)
                })
                .ToList();

            var cityName = cityId.HasValue
                ? (await _db.DormitoryCities.FindAsync(cityId))?.Name ?? ""
                : "الكل";

            return View(new PaymentReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                DormitoryCityID = cityId,
                CityName = cityName,
                TotalCollected = allPayments.Where(p => p.Status == "Completed").Sum(p => p.PaidAmount),
                TotalTransactions = allPayments.Count,
                SuccessfulCount = allPayments.Count(p => p.Status == "Completed"),
                PendingCount = allPayments.Count(p => p.Status == "Pending" || p.Status == "Partial"),
                OverdueCount = allPayments.Count(p => p.Status == "Overdue"),
                Summary = summary
            });
        }

        [HttpGet]
        public async Task<IActionResult> ReportExportExcel(DateTime? fromDate, DateTime? toDate, int? cityId)
        {
            var paymentsQuery = _db.Payments.AsQueryable();
            if (fromDate.HasValue) paymentsQuery = paymentsQuery.Where(p => p.RecordedAt >= fromDate.Value);
            if (toDate.HasValue) paymentsQuery = paymentsQuery.Where(p => p.RecordedAt <= toDate.Value.AddDays(1));
            if (cityId.HasValue) paymentsQuery = paymentsQuery.Where(p => p.Student!.Applications!.Any(a => a.DormitoryCityID == cityId));
            var allPayments = await paymentsQuery.ToListAsync();
            var summary = allPayments.GroupBy(p => p.PaymentType).Select(g => new {
                PaymentType = g.Key, Count = g.Count(), TotalAmount = g.Sum(p => p.Amount), TotalPaid = g.Sum(p => p.PaidAmount)
            }).ToList();
            var columns = new[] { "النوع", "العدد", "الإجمالي", "المحصل" };
            var data = _export.ExportToExcel("تقرير المدفوعات", columns, summary, r => new object?[] { r.PaymentType, r.Count, r.TotalAmount, r.TotalPaid });
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Payments.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ReportExportPdf(DateTime? fromDate, DateTime? toDate, int? cityId)
        {
            var paymentsQuery = _db.Payments.AsQueryable();
            if (fromDate.HasValue) paymentsQuery = paymentsQuery.Where(p => p.RecordedAt >= fromDate.Value);
            if (toDate.HasValue) paymentsQuery = paymentsQuery.Where(p => p.RecordedAt <= toDate.Value.AddDays(1));
            if (cityId.HasValue) paymentsQuery = paymentsQuery.Where(p => p.Student!.Applications!.Any(a => a.DormitoryCityID == cityId));
            var allPayments = await paymentsQuery.ToListAsync();
            var summary = allPayments.GroupBy(p => p.PaymentType).Select(g => new {
                PaymentType = g.Key, Count = g.Count(), TotalAmount = g.Sum(p => p.Amount), TotalPaid = g.Sum(p => p.PaidAmount)
            }).ToList();
            var columns = new[] { "النوع", "العدد", "الإجمالي", "المحصل" };
            var pdfRows = summary.Select(r => new[] { r.PaymentType, r.Count.ToString(), r.TotalAmount.ToString("N2"), r.TotalPaid.ToString("N2") }).ToArray();
            var data = _export.ExportToPdf("تقرير المدفوعات", columns, pdfRows);
            return File(data, "application/pdf", "Payments.pdf");
        }
    }
}
