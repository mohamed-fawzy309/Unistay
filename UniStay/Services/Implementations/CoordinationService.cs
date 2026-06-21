using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations
{
    public class CoordinationService : ICoordinationService
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;

        public CoordinationService(AssuitDbContext db, IAuditService audit, IEmailService email)
        {
            _db = db;
            _audit = audit;
            _email = email;
        }

        // ============================================================
        // Score
        // ============================================================

        public decimal CalculateScore(Application app, Student s, CityConfiguration cfg)
        {
            if (s == null) return 0;

            if (s.HasDisability == true) return 9999;

            // ✅ DateOnly fix
            int age = DateTime.Today.Year - s.BirthDate.Year;

            decimal distance = (s.DistanceFromUniv ?? 0) * 0.40m;
            decimal grade = (s.GradePercentage ?? 0) * 0.40m;

            // ✅ byte fix
            int maxAge = (int)(cfg.MaxAge ?? 0);
            decimal ageScore = Math.Max(0, maxAge - age) * 0.20m;

            decimal bonus = 0;
            if (s.IsOrphan == true) bonus += 50;
            if (s.IsLowIncome == true) bonus += 30;
            if (s.HasFamilyAbroad == true) bonus += 20;

            return distance + grade + ageScore + bonus;
        }

        // ============================================================
        // Preview
        // ============================================================

        public async Task<CoordinationPreview> PreviewAsync(int cityId, string year)
        {
            var cfg = await _db.CityConfigurations
                .FirstAsync(c => c.DormitoryCityID == cityId);

            var apps = await _db.Applications
                .Include(a => a.Student)
                .Where(a => a.DormitoryCityID == cityId &&
                            a.AcademicYear == year &&
                            a.Status == "UnderReview")
                .ToListAsync();

            var scored = apps
                .Where(a => a.Student != null)
                .Select(a => new
                {
                    App = a,
                    Score = CalculateScore(a, a.Student!, cfg)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            // ✅ byte fix
            var beds = await _db.CityRooms
                .Where(r => r.CityBuilding != null &&
                            r.CityBuilding.DormitoryCityID == cityId &&
                            r.IsActive == true &&
                            r.RoomType != "إشراف" && r.RoomType != "مخزن")
                .SumAsync(r => (int)r.BedsCount - (int)r.CurrentOccupancy);

            return new CoordinationPreview
            {
                TotalApplicants = apps.Count,
                AcceptedCount = Math.Min(beds, scored.Count),
                WaitlistCount = Math.Max(0, scored.Count - beds),
                TopStudents = scored
                    .Take(10)
                    .Select(x => new
                    {
                        Name = x.App.Student!.FullName ?? "",
                        Score = x.Score
                    })
                    .ToList<dynamic>()
            };
        }

        // ============================================================
        // Run
        // ============================================================

        public async Task<CoordinationRunResult> RunAsync(int cityId, string year, int userId)
        {
            var preview = await PreviewAsync(cityId, year);

            var apps = await _db.Applications
                .Include(a => a.Student)
                .Where(a => a.DormitoryCityID == cityId &&
                            a.AcademicYear == year &&
                            a.Status == "UnderReview")
                .ToListAsync();

            var cfg = await _db.CityConfigurations
                .FirstAsync(c => c.DormitoryCityID == cityId);

            var sorted = apps
                .Where(a => a.Student != null)
                .OrderByDescending(a => CalculateScore(a, a.Student!, cfg))
                .ToList();

            int accepted = 0;

            foreach (var app in sorted)
            {
                var oldStatus = app.Status;

                app.Status = accepted < preview.AcceptedCount ? "Accepted" : "Waitlist";

                app.CoordinationScore = CalculateScore(app, app.Student!, cfg);

                await _audit.LogAsync(
                    userId,
                    "Staff",
                    "Coordination.Run",
                    "Application",
                    app.ID,
                    new { Status = oldStatus },
                    new { Status = app.Status },
                    null
                );

                if (app.Status == "Accepted" && app.Student != null)
                {
                    await _email.SendAsync(
                        app.Student.Email ?? "",
                        "تم قبول طلبك - UniStay",
                        $"تهانينا {app.Student.FullName}، تم قبولك مبدئياً.",
                        EmailType.ApplicationAccepted,
                        app.StudentID
                    );

                    accepted++;
                }
            }

            await _db.SaveChangesAsync();

            return new CoordinationRunResult
            {
                Accepted = accepted,
                Waitlist = apps.Count - accepted,
                Rejected = 0
            };
        }
    }
}