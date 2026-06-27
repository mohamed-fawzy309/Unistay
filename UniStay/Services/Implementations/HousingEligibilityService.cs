using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations
{
    public class HousingEligibilityService : IHousingEligibilityService
    {
        private readonly AssuitDbContext _db;

        public HousingEligibilityService(AssuitDbContext db)
        {
            _db = db;
        }

        public async Task<EligibilityCheckResult> CheckStudentEligibilityAsync(int studentId, string academicYear)
        {
            var result = new EligibilityCheckResult { IsEligible = true };

            if (await HasPendingViolationsAsync(studentId))
            {
                result.BlockingReasons.Add("الطالب لديه مخالفات غير مسددة");
                result.IsEligible = false;
            }

            if (await HasUnpaidFeesAsync(studentId))
            {
                result.BlockingReasons.Add("الطالب لديه رسوم غير مسددة");
                result.IsEligible = false;
            }

            if (await IsOnBlacklistAsync(studentId))
            {
                result.BlockingReasons.Add("الطالب مدرج في القائمة السوداء");
                result.IsEligible = false;
            }

            if (!await HasCompletedApplicationAsync(studentId, academicYear))
            {
                result.BlockingReasons.Add("لم يتم تقديم طلب سكن للعام الدراسي الحالي");
                result.IsEligible = false;
            }

            if (await IsCurrentlyAllocatedAsync(studentId, academicYear))
            {
                result.BlockingReasons.Add("الطالب لديه تخصيص سكن حالي نشط");
                result.IsEligible = false;
            }

            if (await HasHousingAffectingPenaltiesAsync(studentId))
            {
                result.BlockingReasons.Add("الطالب لديه جزاءات تؤثر على أهلية السكن");
                result.IsEligible = false;
            }

            return result;
        }

        public async Task<bool> HasPendingViolationsAsync(int studentId)
        {
            return await _db.Violations.AnyAsync(v =>
                v.StudentID == studentId &&
                v.Status == "Open" &&
                (v.FineAmount == null || v.FinePaid == null || v.FinePaid < v.FineAmount));
        }

        public async Task<bool> HasUnpaidFeesAsync(int studentId)
        {
            return await _db.StudentFeeRecords.AnyAsync(f =>
                f.StudentID == studentId &&
                f.Status != "Paid" &&
                f.Status != "Cancelled");
        }

        public async Task<bool> IsOnBlacklistAsync(int studentId)
        {
            return await _db.Violations.AnyAsync(v =>
                v.StudentID == studentId &&
                v.IsOnBlacklist == true);
        }

        public async Task<bool> HasCompletedApplicationAsync(int studentId, string academicYear)
        {
            return await _db.Applications.AnyAsync(a =>
                a.StudentID == studentId &&
                a.AcademicYear == academicYear &&
                a.Status == "Approved");
        }

        public async Task<bool> IsCurrentlyAllocatedAsync(int studentId, string academicYear)
        {
            return await _db.Allocations.AnyAsync(a =>
                a.StudentID == studentId &&
                a.AcademicYear == academicYear &&
                a.Status == "Active");
        }

        private async Task<bool> HasHousingAffectingPenaltiesAsync(int studentId)
        {
            return await _db.StudentPenalties.AnyAsync(sp =>
                sp.StudentID == studentId &&
                sp.Status == "Open" &&
                sp.PenaltyType.AffectsHousingEligibility);
        }
    }
}
