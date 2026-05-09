using UniStay.Models;

namespace UniStay.Services.Interfaces
{
    public class CoordinationPreview { public int TotalApplicants { get; set; } public int AcceptedCount { get; set; } public int WaitlistCount { get; set; } public List<dynamic> TopStudents { get; set; } }
    public class CoordinationRunResult { public int Accepted { get; set; } public int Rejected { get; set; } public int Waitlist { get; set; } }

    public interface ICoordinationService
    {
        Task<CoordinationPreview> PreviewAsync(int dormitoryCityId, string academicYear);
        Task<CoordinationRunResult> RunAsync(int dormitoryCityId, string academicYear, int userId);
        decimal CalculateScore(Application app, Student student, CityConfiguration config);
    }
}
