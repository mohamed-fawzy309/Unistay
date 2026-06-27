using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations
{
    public class PenaltyService : IPenaltyService
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;

        public PenaltyService(AssuitDbContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<List<PenaltyType>> GetActivePenaltyTypesAsync()
        {
            return await _db.PenaltyTypes
                .Where(p => p.IsActive)
                .ToListAsync();
        }

        public async Task<PenaltyType?> GetPenaltyTypeByIdAsync(int id)
        {
            return await _db.PenaltyTypes.FindAsync(id);
        }

        public async Task<StudentPenalty> IssuePenaltyAsync(int studentId, int penaltyTypeId, decimal? fineAmount, string? description, int recordedBy, int? dormitoryCityId)
        {
            var penaltyType = await _db.PenaltyTypes.FindAsync(penaltyTypeId)
                ?? throw new ArgumentException("Invalid penalty type");

            var penalty = new StudentPenalty
            {
                StudentID = studentId,
                PenaltyTypeID = penaltyTypeId,
                DormitoryCityID = dormitoryCityId,
                FineAmount = fineAmount ?? penaltyType.DefaultFineAmount,
                FinePaid = 0,
                Status = "Open",
                Description = description,
                RecordedBy = recordedBy,
                RecordedAt = DateTime.UtcNow
            };

            _db.StudentPenalties.Add(penalty);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(recordedBy, "Staff", "Penalty.Issued", "StudentPenalty", penalty.ID,
                null, new { studentId, penaltyTypeId, fineAmount = penalty.FineAmount });

            return penalty;
        }

        public async Task<bool> ResolvePenaltyAsync(int penaltyId, string resolutionNotes, int resolvedBy)
        {
            var penalty = await _db.StudentPenalties.FindAsync(penaltyId);
            if (penalty == null || penalty.Status != "Open")
                return false;

            penalty.Status = "Resolved";
            penalty.ResolvedBy = resolvedBy;
            penalty.ResolvedAt = DateTime.UtcNow;
            penalty.ResolutionNotes = resolutionNotes;

            await _db.SaveChangesAsync();
            await _audit.LogAsync(resolvedBy, "Staff", "Penalty.Resolved", "StudentPenalty", penaltyId);
            return true;
        }

        public async Task<List<StudentPenalty>> GetStudentPenaltiesAsync(int studentId)
        {
            return await _db.StudentPenalties
                .Where(p => p.StudentID == studentId)
                .Include(p => p.PenaltyType)
                .Include(p => p.RecordedByNavigation)
                .Include(p => p.ResolvedByNavigation)
                .OrderByDescending(p => p.RecordedAt)
                .ToListAsync();
        }

        public async Task<StudentPenalty?> GetPenaltyByIdAsync(int id)
        {
            return await _db.StudentPenalties
                .Include(p => p.PenaltyType)
                .Include(p => p.Student)
                .Include(p => p.RecordedByNavigation)
                .FirstOrDefaultAsync(p => p.ID == id);
        }

        public async Task<List<StudentPenalty>> GetOpenPenaltiesAsync(int studentId)
        {
            return await _db.StudentPenalties
                .Where(p => p.StudentID == studentId && p.Status == "Open")
                .Include(p => p.PenaltyType)
                .ToListAsync();
        }

        public async Task<bool> HasHousingAffectingPenaltiesAsync(int studentId)
        {
            return await _db.StudentPenalties.AnyAsync(p =>
                p.StudentID == studentId &&
                p.Status == "Open" &&
                p.PenaltyType.AffectsHousingEligibility);
        }
    }
}
