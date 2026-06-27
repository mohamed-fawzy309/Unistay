namespace UniStay.Services.Interfaces
{
    public class EligibilityCheckResult
    {
        public bool IsEligible { get; set; }
        public List<string> BlockingReasons { get; set; } = new();
    }

    public interface IHousingEligibilityService
    {
        Task<EligibilityCheckResult> CheckStudentEligibilityAsync(int studentId, string academicYear);
        Task<bool> HasPendingViolationsAsync(int studentId);
        Task<bool> HasUnpaidFeesAsync(int studentId);
        Task<bool> IsOnBlacklistAsync(int studentId);
        Task<bool> HasCompletedApplicationAsync(int studentId, string academicYear);
        Task<bool> IsCurrentlyAllocatedAsync(int studentId, string academicYear);
    }
}
