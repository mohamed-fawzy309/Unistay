using Microsoft.EntityFrameworkCore;
using System;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations
{
    public class MealService(AssuitDbContext db) : IMealService
    {
        public async Task GenerateDailyMealsAsync(int cityId, DateTime date)
        {
            var d = DateOnly.FromDateTime(date);

            var bookedMeals = await db.Meals
                .Where(m => m.DormitoryCityID == cityId
                    && m.MealDate == d
                    && m.IsBooked == true
                    && m.IsActive == true)
                .ToListAsync();

            var blockedIds = await db.MealBlocks
                .Where(b => (b.IsActive ?? false)
                    && d >= b.FromDate && d <= b.ToDate)
                .Select(b => b.StudentID)
                .Distinct()
                .ToListAsync();

            var cancelledCount = 0;
            foreach (var meal in bookedMeals)
            {
                if (blockedIds.Contains(meal.StudentID))
                {
                    meal.IsActive = false;
                    meal.CancelReason = "محظور";
                    cancelledCount++;
                }
            }

            if (cancelledCount > 0)
                await db.SaveChangesAsync();
        }

        public async Task CancelBulkMealsAsync(int cityId, DateTime from, DateTime to, string reason)
        {
            var fromD = DateOnly.FromDateTime(from);
            var toD = DateOnly.FromDateTime(to);

            db.MealCancellations.Add(new MealCancellation { DormitoryCityID = cityId, FromDate = fromD, ToDate = toD, CancellationType = "Bulk", CreatedAt = DateTime.Now });

            await db.Meals
                .Where(m => m.DormitoryCityID == cityId && m.MealDate >= fromD && m.MealDate <= toD && !(m.IsConsumed ?? false))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.IsActive, false)
                    .SetProperty(m => m.CancelReason, reason));

            await db.SaveChangesAsync();
        }

        public async Task<bool> CanConsumeAsync(int studentId, int cityId, DateTime date)
        {
            var d = DateOnly.FromDateTime(date);
            var blocked = await db.MealBlocks.AnyAsync(b =>
                b.StudentID == studentId && (b.IsActive ?? false) &&
                d >= b.FromDate && d <= b.ToDate);
            if (blocked) return false;

            return await db.Meals.AnyAsync(m =>
                m.StudentID == studentId &&
                m.DormitoryCityID == cityId &&
                m.MealDate == d &&
                (m.IsBooked ?? false) &&
                !(m.IsConsumed ?? false) &&
                (m.IsActive ?? true));
        }

        public async Task<bool> CanConsumeByTypeAsync(int studentId, int cityId, DateTime date, string mealType)
        {
            var d = DateOnly.FromDateTime(date);
            var blocked = await db.MealBlocks.AnyAsync(b =>
                b.StudentID == studentId && (b.IsActive ?? false) &&
                d >= b.FromDate && d <= b.ToDate &&
                (b.MealType == null || b.MealType == mealType));
            if (blocked) return false;

            return await db.Meals.AnyAsync(m =>
                m.StudentID == studentId &&
                m.DormitoryCityID == cityId &&
                m.MealDate == d &&
                m.MealType == mealType &&
                (m.IsBooked ?? false) &&
                !(m.IsConsumed ?? false) &&
                (m.IsActive ?? true));
        }
    }
}
