using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations
{
    public class FeeService : IFeeService
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;

        public FeeService(AssuitDbContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<List<HousingFeeTemplate>> GetApplicableTemplatesAsync(int? dormitoryCityId, string academicYear)
        {
            return await _db.HousingFeeTemplates
                .Where(t => t.IsActive == true &&
                            t.AcademicYear == academicYear &&
                            (t.DormitoryCityID == null || t.DormitoryCityID == dormitoryCityId))
                .Include(t => t.FeeType)
                .ToListAsync();
        }

        public async Task<FeeSplitResult> CalculateFeeSplitAsync(int templateId, int installmentCount = 1)
        {
            var template = await _db.HousingFeeTemplates.FindAsync(templateId);
            if (template == null)
                return new FeeSplitResult();

            installmentCount = Math.Max(1, installmentCount);
            var installmentAmount = Math.Round(template.Amount / installmentCount, 2);

            var dueDates = new List<DateOnly>();
            var startDate = DateOnly.FromDateTime(DateTime.Today);
            for (int i = 0; i < installmentCount; i++)
            {
                dueDates.Add(startDate.AddMonths(i));
            }

            return new FeeSplitResult
            {
                TotalAmount = template.Amount,
                InstallmentAmount = installmentAmount,
                InstallmentCount = installmentCount,
                DueDates = dueDates
            };
        }

        public async Task<bool> GenerateStudentFeeRecordsAsync(int studentId, int allocationId, int templateId)
        {
            var template = await _db.HousingFeeTemplates
                .Include(t => t.FeeType)
                .FirstOrDefaultAsync(t => t.ID == templateId);

            var allocation = await _db.Allocations.FindAsync(allocationId);
            if (template == null || allocation == null)
                return false;

            var split = await CalculateFeeSplitAsync(templateId, template.InstallmentCount);
            var monthNames = new[] { "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر", "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو" };

            foreach (var (installment, index) in split.DueDates.Select((date, i) => (date, i)))
            {
                var monthYear = index < monthNames.Length
                    ? $"{monthNames[index]} {installment.Year}"
                    : $"قسط {index + 1} {installment.Year}";

                var record = new StudentFeeRecord
                {
                    StudentID = studentId,
                    HousingFeeTemplateID = templateId,
                    AllocationID = allocationId,
                    TotalAmount = split.InstallmentAmount,
                    PaidAmount = 0,
                    InstallmentNumber = index + 1,
                    TotalInstallments = split.InstallmentCount,
                    DueDate = installment,
                    Status = "Pending",
                    MonthYear = monthYear,
                    RecordedAt = DateTime.UtcNow
                };

                _db.StudentFeeRecords.Add(record);
            }

            await _db.SaveChangesAsync();
            await _audit.LogAsync(0, "System", "FeeRecords.Generated", "StudentFeeRecord", studentId);
            return true;
        }

        public async Task<bool> RecordPaymentAsync(int feeRecordId, decimal amount, int recordedBy)
        {
            var record = await _db.StudentFeeRecords.FindAsync(feeRecordId);
            if (record == null || record.Status == "Paid" || record.Status == "Cancelled")
                return false;

            record.PaidAmount += amount;
            record.Status = record.PaidAmount >= record.TotalAmount ? "Paid" : "Partial";
            if (record.Status == "Paid")
                record.PaidAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await _audit.LogAsync(recordedBy, "Staff", "FeeRecord.Payment", "StudentFeeRecord", feeRecordId,
                null, new { record.PaidAmount, record.Status });
            return true;
        }

        public async Task<decimal> GetTotalDueAsync(int studentId)
        {
            return await _db.StudentFeeRecords
                .Where(f => f.StudentID == studentId && f.Status != "Cancelled")
                .SumAsync(f => f.TotalAmount);
        }

        public async Task<decimal> GetOutstandingBalanceAsync(int studentId)
        {
            return await _db.StudentFeeRecords
                .Where(f => f.StudentID == studentId && f.Status != "Cancelled")
                .SumAsync(f => f.TotalAmount - f.PaidAmount);
        }

        public async Task<bool> UpdateMealSubscriptionAsync(int studentId, bool subscribed, string academicYear)
        {
            var application = await _db.Applications
                .FirstOrDefaultAsync(a => a.StudentID == studentId && a.AcademicYear == academicYear);

            if (application == null)
                return false;

            application.MealSubscription = subscribed;
            application.LastUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            if (subscribed)
            {
                var mealTypes = await _db.MealTypes.Where(mt => mt.IsActive == true).ToListAsync();
                foreach (var mealType in mealTypes)
                {
                    var existing = await _db.MealBlocks
                        .FirstOrDefaultAsync(mb => mb.StudentID == studentId &&
                            mb.MealType == mealType.Name);

                    if (existing == null)
                    {
                        _db.MealBlocks.Add(new MealBlock
                        {
                            StudentID = studentId,
                            DormitoryCityID = application.DormitoryCityID,
                            MealType = mealType.Name,
                            FromDate = DateOnly.FromDateTime(DateTime.Today),
                            ToDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
                            Reason = "تفعيل الاشتراك في الوجبات",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _db.SaveChangesAsync();
            }

            return true;
        }

        public async Task<List<StudentFeeRecord>> GetStudentFeeRecordsAsync(int studentId)
        {
            return await _db.StudentFeeRecords
                .Where(f => f.StudentID == studentId)
                .Include(f => f.HousingFeeTemplate)
                    .ThenInclude(t => t.FeeType)
                .OrderBy(f => f.DueDate)
                .ToListAsync();
        }
    }
}
