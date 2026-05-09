namespace UniStay.Services.Interfaces
{
    public class StudentApiResult
    {
        public bool Found { get; set; }
        public string FullName { get; set; }
        public string Faculty { get; set; }
        public int AcademicYear { get; set; }
        public decimal GradePercentage { get; set; }
        public bool IsEnrolled { get; set; }
        public bool HasDebts { get; set; }
        public bool IsMatch { get; set; }
        public Dictionary<string, (object Local, object Server)> Differences { get; set; } = new();
    }
    public class StaffApiResult { public bool Found { get; set; } public string FullName { get; set; } public string JobTitle { get; set; } }
    public class BulkValidationResult { public int Success { get; set; } public int Failed { get; set; } }

    public interface IUniversityApiService
    {
        Task<StudentApiResult> SearchByNationalIDAsync(string nationalId);
        Task<StaffApiResult> SearchStaffByNationalIDAsync(string nationalId);
        Task<BulkValidationResult> BulkValidateAsync(List<string> nationalIds);
    }
}