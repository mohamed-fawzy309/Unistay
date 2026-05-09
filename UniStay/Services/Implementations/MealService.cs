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
            var d = DateOnly.FromDateTime(date); // <-- الحل

            var students = await db.Allocations
                .Include(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
                .Where(a => a.Status == "Active" && a.CityRoom.CityBuilding.DormitoryCityID == cityId)
                .ToListAsync();

            foreach (var alloc in students)
            {
                var hasBlock = await db.MealBlocks.AnyAsync(b =>
                    b.StudentID == alloc.StudentID &&
                    (b.IsActive ?? false) &&
                    d >= b.FromDate && d <= b.ToDate);

                if (hasBlock) continue;

                if (!await db.Meals.AnyAsync(m => m.StudentID == alloc.StudentID && m.MealDate == d))
                {
                    db.Meals.AddRange(
                        new Meal { StudentID = alloc.StudentID, DormitoryCityID = cityId, MealDate = d, MealType = "Lunch", Price = 15, IsBooked = true, IsConsumed = false, IsActive = true },
                        new Meal { StudentID = alloc.StudentID, DormitoryCityID = cityId, MealDate = d, MealType = "Dinner", Price = 10, IsBooked = true, IsConsumed = false, IsActive = true }
                    );
                }
            }
            await db.SaveChangesAsync();
        }

        public async Task CancelBulkMealsAsync(int cityId, DateTime from, DateTime to, string reason)
        {
            var fromD = DateOnly.FromDateTime(from);
            var toD = DateOnly.FromDateTime(to);

            db.MealCancellations.Add(new MealCancellation { DormitoryCityID = cityId, FromDate = fromD, ToDate = toD, CancellationType = "Bulk", CreatedAt = DateTime.Now });

            var meals = await db.Meals
                .Where(m => m.DormitoryCityID == cityId && m.MealDate >= fromD && m.MealDate <= toD && !(m.IsConsumed ?? false))
                .ToListAsync();

            meals.ForEach(m => { m.IsActive = false; m.CancelReason = reason; });
            await db.SaveChangesAsync();
        }

        public async Task<bool> CanConsumeAsync(int studentId, int cityId, DateTime date)
        {
            var d = DateOnly.FromDateTime(date);
            var blocked = await db.MealBlocks.AnyAsync(b => b.StudentID == studentId && (b.IsActive ?? false) && d >= b.FromDate && d <= b.ToDate);
            if (blocked) return false;

            return await db.Meals.AnyAsync(m =>
                m.StudentID == studentId &&
                m.DormitoryCityID == cityId &&
                m.MealDate == d &&
                (m.IsBooked ?? false) &&
                !(m.IsConsumed ?? false) &&
                (m.IsActive ?? true));
        }
    }
}
