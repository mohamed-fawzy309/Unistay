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

    public async Task<int> GetBookedCountInMonthAsync(int studentId, int month, int year)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);
        return await db.Meals
            .CountAsync(m => m.StudentID == studentId
                && m.IsBooked == true
                && m.MealDate >= start
                && m.MealDate < end);
    }

    public async Task<List<DateOnly>> GetBookedDaysInMonthAsync(int studentId, int month, int year)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);
        return await db.Meals
            .Where(m => m.StudentID == studentId
                && m.IsBooked == true
                && m.MealDate >= start
                && m.MealDate < end)
            .Select(m => m.MealDate)
            .Distinct()
            .ToListAsync();
    }

    public async Task<(bool canBook, string message)> CanBookDateAsync(int studentId, DateOnly date)
    {
        if (!IsBeforeDeadline(date))
            return (false, "انتهت مهلة الحجز لهذا اليوم (آخر موعد قبل اليوم السابق الساعة 11 صباحاً)");

        var student = await db.Students.FindAsync(studentId);
        if (student == null)
            return (false, "الطالب غير موجود");

        var allocation = await db.Allocations
            .Include(a => a.Application)
            .FirstOrDefaultAsync(a => a.StudentID == studentId && a.Status == "Active");

        if (allocation == null)
            return (false, "الطالب غير مسكن");

        if (allocation.Application.MealSubscription != true)
            return (false, "الطالب غير مشترك في الوجبات");

        var hasRestriction = await db.MealBlocks.AnyAsync(b =>
            b.StudentID == studentId && b.IsActive == true &&
            date >= b.FromDate && date <= b.ToDate);

        if (hasRestriction)
            return (false, "الطالب محظور من حجز الوجبات");

        var alreadyBooked = await db.Meals.AnyAsync(m =>
            m.StudentID == studentId && m.MealDate == date && m.IsBooked == true);

        if (alreadyBooked)
            return (false, "هذا اليوم محجوز بالفعل");

        var currentMonthYear = GetAcademicMonthYear(DateTime.UtcNow.Month, DateTime.UtcNow.Year);
        var dateMonthYear = GetAcademicMonthYear(date.Month, date.Year);

        if (dateMonthYear != currentMonthYear)
        {
            var nextMonth = DateTime.UtcNow.AddMonths(1);
            var nextMonthYear = GetAcademicMonthYear(nextMonth.Month, nextMonth.Year);

            if (dateMonthYear == nextMonthYear)
            {
                var isPaid = await IsMonthPaidAsync(studentId, DateTime.UtcNow.Month, DateTime.UtcNow.Year);
                if (!isPaid)
                    return (false, "يجب دفع رسوم الشهر الحالي أولاً لحجز وجبات الشهر القادم");
            }
            else
            {
                return (false, "يمكن الحجز للشهر الحالي والشهر التالي فقط");
            }
        }

        var count = await GetBookedCountInMonthAsync(studentId, date.Month, date.Year);
        if (count >= 4)
            return (false, "لقد استنفدت الحد الأقصى لحجز الوجبات لهذا الشهر (4 أيام)");

        return (true, "");
    }

    public async Task<(bool success, string message)> BookDateAsync(int studentId, DateOnly date, int dormitoryCityId)
    {
        var (canBook, msg) = await CanBookDateAsync(studentId, date);
        if (!canBook)
            return (false, msg);

        var existing = await db.Meals.FirstOrDefaultAsync(m =>
            m.StudentID == studentId && m.MealDate == date);

        if (existing != null)
        {
            if (existing.IsBooked == true)
                return (false, "هذا اليوم محجوز بالفعل");
            existing.IsBooked = true;
            existing.IsActive = true;
        }
        else
        {
            db.Meals.Add(new Meal
            {
                StudentID = studentId,
                DormitoryCityID = dormitoryCityId,
                MealDate = date,
                MealType = "General",
                Price = 25m,
                IsBooked = true,
                IsConsumed = false,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
        await audit.LogAsync(studentId, "Student", "MealBooking.BookDate", "Meal",
            null, null, new { studentId, MealDate = date.ToString() });

        return (true, "تم حجز الوجبة بنجاح");
    }

    public async Task<(bool success, string message)> UnbookDateAsync(int studentId, DateOnly date)
    {
        if (!IsBeforeDeadline(date))
            return (false, "لا يمكن إلغاء الحجز بعد انتهاء المهلة (آخر موعد قبل اليوم السابق الساعة 11 صباحاً)");

        var meal = await db.Meals.FirstOrDefaultAsync(m =>
            m.StudentID == studentId && m.MealDate == date && m.IsBooked == true);

        if (meal == null)
            return (false, "لا يوجد حجز لهذا اليوم");

        meal.IsBooked = false;
        meal.IsActive = false;
        await db.SaveChangesAsync();

        await audit.LogAsync(studentId, "Student", "MealBooking.UnbookDate", "Meal",
            meal.ID, new { IsBooked = true }, new { IsBooked = false });

        return (true, "تم إلغاء الحجز بنجاح");
    }

    public async Task<bool> IsMonthPaidAsync(int studentId, int month, int year)
    {
        var monthYear = GetAcademicMonthYear(month, year);
        return await db.StudentFeeRecords
            .AnyAsync(f => f.StudentID == studentId
                && f.MonthYear == monthYear
                && f.Status == "Paid");
    }

    public async Task<(string currentMonthYear, string nextMonthYear)> GetBookingMonthsAsync(int studentId)
    {
        var now = DateTime.UtcNow;
        var current = GetAcademicMonthYear(now.Month, now.Year);
        var nextMonth = now.AddMonths(1);
        var next = GetAcademicMonthYear(nextMonth.Month, nextMonth.Year);
        return (current, next);
    }

    public async Task<List<Meal>> GetBookedMealsAsync(int studentId)
    {
        return await db.Meals
            .Where(m => m.StudentID == studentId && m.IsBooked == true)
            .OrderByDescending(m => m.MealDate)
            .ToListAsync();
    }

    public async Task<string> GetDeadlineDisplayAsync()
    {
        return await Task.FromResult("آخر موعد للحجز أو الإلغاء هو الساعة 11 صباحاً من اليوم السابق لتاريخ الوجبة");
    }

    private static bool IsBeforeDeadline(DateOnly date)
    {
        var deadline = new DateTime(date.Year, date.Month, date.Day, 11, 0, 0, DateTimeKind.Utc).AddDays(-1);
        return DateTime.UtcNow < deadline;
    }

    private static string GetAcademicMonthYear(int month, int year)
    {
        var months = new[] { "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر", "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس" };
        var label = months[(month + 3) % 12];
        return $"{label} {year}";
    }
}
