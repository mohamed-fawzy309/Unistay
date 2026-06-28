using UniStay.Models;

namespace UniStay.Services.Interfaces
{
    public class FeeSplitResult
    {
        public decimal TotalAmount { get; set; }
        public decimal InstallmentAmount { get; set; }
        public int InstallmentCount { get; set; }
        public List<DateOnly> DueDates { get; set; } = new();
    }

    public interface IFeeService
    {
        Task<List<HousingFeeTemplate>> GetApplicableTemplatesAsync(int? dormitoryCityId, string academicYear);
        Task<FeeSplitResult> CalculateFeeSplitAsync(int templateId, int installmentCount = 1);
        Task<bool> GenerateStudentFeeRecordsAsync(int studentId, int allocationId, int templateId);
        Task<bool> RecordPaymentAsync(int feeRecordId, decimal amount, int recordedBy);
        Task<decimal> GetTotalDueAsync(int studentId);
        Task<decimal> GetOutstandingBalanceAsync(int studentId);
        Task<bool> UpdateMealSubscriptionAsync(int studentId, bool subscribed, string academicYear);
        Task<List<StudentFeeRecord>> GetStudentFeeRecordsAsync(int studentId);
        Task<bool> IsMonthPaidAsync(int studentId, string monthYear);
    }
}
