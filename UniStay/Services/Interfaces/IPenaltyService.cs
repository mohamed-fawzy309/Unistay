using UniStay.Models;

namespace UniStay.Services.Interfaces
{
    public interface IPenaltyService
    {
        Task<List<PenaltyType>> GetActivePenaltyTypesAsync();
        Task<PenaltyType?> GetPenaltyTypeByIdAsync(int id);
        Task<StudentPenalty> IssuePenaltyAsync(int studentId, int penaltyTypeId, decimal? fineAmount, string? description, int recordedBy, int? dormitoryCityId);
        Task<bool> ResolvePenaltyAsync(int penaltyId, string resolutionNotes, int resolvedBy);
        Task<List<StudentPenalty>> GetStudentPenaltiesAsync(int studentId);
        Task<StudentPenalty?> GetPenaltyByIdAsync(int id);
        Task<List<StudentPenalty>> GetOpenPenaltiesAsync(int studentId);
        Task<bool> HasHousingAffectingPenaltiesAsync(int studentId);
    }
}
