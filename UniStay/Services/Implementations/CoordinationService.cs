using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Coordination;

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

        private static decimal ComputeBonus(Student s)
        {
            decimal bonus = 0;
            if (s.IsOrphan == true) bonus += 50;
            if (s.IsLowIncome == true) bonus += 30;
            if (s.HasFamilyAbroad == true) bonus += 20;
            return bonus;
        }

        public ScoreComponents CalculateScore(Application app, Student s, CityConfiguration cfg, List<CoordinationRule> rules)
        {
            if (s == null) return new ScoreComponents();

            int age = DateTime.Today.Year - s.BirthDate.Year;
            int maxAge = (int)(cfg.MaxAge ?? 0);

            var activeRules = rules.Where(r => r.IsActive == true).OrderBy(r => r.Priority).ThenBy(r => r.ID).ToList();

            // Disability always gets priority score
            if (s.HasDisability == true)
            {
                var specialRule = activeRules.FirstOrDefault(r => r.RuleType == CoordinationRuleTypes.Special);
                decimal bonus = specialRule != null
                    ? (specialRule.Weight / 100m) * ComputeBonus(s)
                    : 0;
                return new ScoreComponents { BonusScore = 9999 + bonus };
            }

            if (activeRules.Count == 0)
            {
                return new ScoreComponents
                {
                    DistanceScore = (s.DistanceFromUniv ?? 0) * 0.40m,
                    GradeScore = (s.GradePercentage ?? 0) * 0.40m,
                    AgeScore = Math.Max(0, maxAge - age) * 0.20m,
                    BonusScore = ComputeBonus(s)
                };
            }

            var components = new ScoreComponents();

            foreach (var rule in activeRules)
            {
                decimal raw = rule.RuleType switch
                {
                    CoordinationRuleTypes.Distance => s.DistanceFromUniv ?? 0,
                    CoordinationRuleTypes.Grade => s.GradePercentage ?? 0,
                    CoordinationRuleTypes.Age => Math.Max(0, maxAge - age),
                    CoordinationRuleTypes.Bonus => ComputeBonus(s),
                    CoordinationRuleTypes.Faculty => 
                        (!string.IsNullOrEmpty(s.Faculty) && !string.IsNullOrEmpty(rule.RuleName) && 
                         (s.Faculty.Replace("كلية ", "").Replace("معهد ", "").Trim().Contains(rule.RuleName.Replace("كلية ", "").Replace("معهد ", "").Trim(), StringComparison.OrdinalIgnoreCase) || 
                          rule.RuleName.Replace("كلية ", "").Replace("معهد ", "").Trim().Contains(s.Faculty.Replace("كلية ", "").Replace("معهد ", "").Trim(), StringComparison.OrdinalIgnoreCase))) ? 100m : 0m,
                    _ => 0
                };

                decimal weighted = raw * (rule.Weight / 100m);

                switch (rule.RuleType)
                {
                    case CoordinationRuleTypes.Distance: components.DistanceScore += weighted; break;
                    case CoordinationRuleTypes.Grade: components.GradeScore += weighted; break;
                    case CoordinationRuleTypes.Age: components.AgeScore += weighted; break;
                    default: components.BonusScore += weighted; break;
                }
            }

            return components;
        }

        private async Task<List<CoordinationRule>> LoadActiveRulesAsync(int cityId)
        {
            return await _db.CoordinationRules
                .Where(r => r.DormitoryCityID == cityId)
                .OrderBy(r => r.Priority).ThenBy(r => r.ID)
                .ToListAsync();
        }

        public async Task<CoordinationPreview> PreviewAsync(int cityId, string year)
        {
            var cfg = await _db.CityConfigurations
                .FirstOrDefaultAsync(c => c.DormitoryCityID == cityId);
            if (cfg == null)
                throw new InvalidOperationException("لم يتم إعداد إعدادات المدينة بعد");

            var rules = await LoadActiveRulesAsync(cityId);

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
                    Score = CalculateScore(a, a.Student!, cfg, rules).Total
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            var beds = await _db.CityRooms
                .Where(r => r.CityBuilding != null &&
                            r.CityBuilding.DormitoryCityID == cityId &&
                            r.IsActive == true &&
                            r.RoomType != "إشراف" && r.RoomType != "مخزن")
                .SumAsync(r => Convert.ToInt32(r.BedsCount) - Convert.ToInt32(r.CurrentOccupancy));

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
                .FirstOrDefaultAsync(c => c.DormitoryCityID == cityId);
            if (cfg == null)
                throw new InvalidOperationException("لم يتم إعداد إعدادات المدينة بعد");

            var rules = await LoadActiveRulesAsync(cityId);

            // remove old results for re-run
            var oldResults = await _db.CoordinationResults
                .Where(r => r.DormitoryCityID == cityId && r.AcademicYear == year)
                .ToListAsync();
            if (oldResults.Count > 0)
                _db.CoordinationResults.RemoveRange(oldResults);

            var scored = apps
                .Where(a => a.Student != null)
                .Select(a => new
                {
                    App = a,
                    Components = CalculateScore(a, a.Student!, cfg, rules)
                })
                .OrderByDescending(x => x.Components.Total)
                .ToList();

            int rank = 0;
            int accepted = 0;

            foreach (var item in scored)
            {
                rank++;
                var app = item.App;
                var oldStatus = app.Status;

                app.Status = accepted < preview.AcceptedCount ? "Accepted" : "Waitlist";
                app.CoordinationScore = item.Components.Total;
                app.CoordinationRank = rank;

                var result = new CoordinationResult
                {
                    ApplicationID = app.ID,
                    StudentID = app.StudentID,
                    DormitoryCityID = cityId,
                    AcademicYear = year,
                    DistanceScore = item.Components.DistanceScore,
                    GradeScore = item.Components.GradeScore,
                    AgeScore = item.Components.AgeScore,
                    SpecialBonus = item.Components.BonusScore,
                    TotalScore = item.Components.Total,
                    Rank = rank,
                    Status = app.Status,
                    ProcessedAt = DateTime.UtcNow,
                    ProcessedBy = userId
                };
                _db.CoordinationResults.Add(result);

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
