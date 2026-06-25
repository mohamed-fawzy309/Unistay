using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Helpers;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Meal;

namespace UniStay.Services.Implementations;

public class MealBookingService(AssuitDbContext db, IAuditService audit) : IMealBookingService
{
    public async Task<ScanBookingResultViewModel?> ScanStudentAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return null;

        var student = await StudentLookupHelper.FindStudentWithAllocationAsync(db, searchTerm);

        if (student == null)
            return new ScanBookingResultViewModel
            {
                IsEligible = false,
                EligibilityMessage = "الطالب غير موجود"
            };

        var cityName = StudentLookupHelper.GetStudentCityName(student);
        var cityId = student.Allocations.FirstOrDefault()?.CityRoom?.CityBuilding?.DormitoryCityID ?? 0;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var (hasRestriction, restriction) = await StudentLookupHelper.GetActiveRestrictionAsync(db, student.ID, today);

        return new ScanBookingResultViewModel
        {
            StudentID = student.ID,
            StudentName = student.FullName,
            NationalID = student.NationalID,
            CityName = cityName,
            DormitoryCityID = cityId,
            IsEligible = !hasRestriction,
            EligibilityMessage = hasRestriction ? "الطالب محظور من الوجبات" : "يمكن حجز الوجبات",
            RestrictionReason = restriction?.Reason
        };
    }

    public async Task<(bool success, string message)> BookMealAsync(BookMealViewModel model, int userId)
    {
        var student = await db.Students.FindAsync(model.StudentID);
        if (student == null)
            return (false, "الطالب غير موجود");

        var allocation = await db.Allocations
            .Include(a => a.Application)
            .FirstOrDefaultAsync(a => a.StudentID == model.StudentID && a.Status == "Active");

        if (allocation == null)
            return (false, "الطالب غير مسكن");

        if (allocation.Application.MealSubscription != true)
            return (false, "الطالب غير مشترك في الوجبات");

        var hasRestriction = await db.MealBlocks.AnyAsync(b =>
            b.StudentID == model.StudentID && b.IsActive == true &&
            model.MealDate >= b.FromDate && model.MealDate <= b.ToDate);

        if (hasRestriction)
            return (false, "الطالب محظور من حجز الوجبات");

        var existing = await db.Meals.AnyAsync(m =>
            m.StudentID == model.StudentID && m.MealDate == model.MealDate && m.IsBooked == true);

        if (existing)
            return (false, "الوجبة محجوزة بالفعل لهذا اليوم");

        var meal = new Meal
        {
            StudentID = model.StudentID,
            DormitoryCityID = model.DormitoryCityID,
            MealDate = model.MealDate,
            MealType = "General",
            Price = 25m,
            IsBooked = true,
            IsConsumed = false,
            IsActive = true
        };

        db.Meals.Add(meal);
        await db.SaveChangesAsync();

        await audit.LogAsync(userId, "Staff", "MealBooking.Create", "Meal",
            meal.ID, null, new { model.StudentID, model.MealDate });

        return (true, "تم حجز الوجبة بنجاح");
    }

    public async Task<(int successCount, List<string> errors)> BookDatesAsync(BookDatesViewModel model, int userId)
    {
        var student = await db.Students.FindAsync(model.StudentID);
        if (student == null)
            return (0, new List<string> { "الطالب غير موجود" });

        var allocation = await db.Allocations
            .Include(a => a.Application)
            .FirstOrDefaultAsync(a => a.StudentID == model.StudentID && a.Status == "Active");

        if (allocation == null)
            return (0, new List<string> { "الطالب غير مسكن" });

        if (allocation.Application.MealSubscription != true)
            return (0, new List<string> { "الطالب غير مشترك في الوجبات" });

        var existingDates = await db.Meals
            .Where(m => m.StudentID == model.StudentID && m.IsBooked == true)
            .Select(m => m.MealDate)
            .ToListAsync();

        const int maxDays = 4;
        if (existingDates.Count + model.SelectedDates.Count > maxDays)
        {
            return (0, new List<string> { $"لا يمكن حجز أكثر من {maxDays} أيام إجمالاً" });
        }

        var blockedDates = await db.MealBlocks
            .Where(b => b.StudentID == model.StudentID && b.IsActive == true)
            .Select(b => new { b.FromDate, b.ToDate })
            .ToListAsync();

        var errors = new List<string>();
        var successCount = 0;
        var mealsToAdd = new List<Meal>();

        foreach (var date in model.SelectedDates)
        {
            if (existingDates.Contains(date))
            {
                errors.Add($"اليوم {date:yyyy-MM-dd}: محجوز بالفعل");
                continue;
            }

            if (blockedDates.Any(b => date >= b.FromDate && date <= b.ToDate))
            {
                errors.Add($"اليوم {date:yyyy-MM-dd}: الطالب محظور");
                continue;
            }

            mealsToAdd.Add(new Meal
            {
                StudentID = model.StudentID,
                DormitoryCityID = model.DormitoryCityID,
                MealDate = date,
                MealType = "General",
                Price = 25m,
                IsBooked = true,
                IsConsumed = false,
                IsActive = true
            });
            successCount++;
        }

        if (mealsToAdd.Count > 0)
        {
            db.Meals.AddRange(mealsToAdd);
            await db.SaveChangesAsync();
        }

        await audit.LogAsync(userId, "Staff", "MealBooking.BookDates", "Meal",
            null, null, new { model.StudentID, Count = successCount, Errors = errors.Count });

        return (successCount, errors);
    }

    public async Task<List<DateOnly>> GetBookedDatesAsync(int studentId)
    {
        return await db.Meals
            .Where(m => m.StudentID == studentId && m.IsBooked == true && (m.IsActive ?? true))
            .Select(m => m.MealDate)
            .Distinct()
            .ToListAsync();
    }

}
