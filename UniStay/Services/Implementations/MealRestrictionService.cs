using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Meal;

namespace UniStay.Services.Implementations;

public class MealRestrictionService(AssuitDbContext db, IAuditService audit) : IMealRestrictionService
{
    private const int PageSize = 20;

    public async Task<MealRestrictionIndexViewModel> GetRestrictionsAsync(string? tab, int? cityId, string? mealType, string? search, int page)
    {
        var cities = await db.DormitoryCities.Where(c => c.IsActive)
            .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync();

        var mealTypes = await db.Meals.Select(m => m.MealType).Distinct().ToListAsync();

        var query = db.MealBlocks.Include(b => b.Student).Include(b => b.DormitoryCity).Include(b => b.CreatedByNavigation).AsQueryable();

        if (tab == "active")
            query = query.Where(b => b.IsActive == true && b.ToDate >= DateOnly.FromDateTime(DateTime.Today));
        else if (tab == "expired")
            query = query.Where(b => b.IsActive == false || b.ToDate < DateOnly.FromDateTime(DateTime.Today));

        if (cityId.HasValue)
            query = query.Where(b => b.DormitoryCityID == cityId.Value);
        if (!string.IsNullOrEmpty(mealType))
            query = query.Where(b => b.MealType == mealType || b.MealType == null);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(b => b.Student.FullName.Contains(search) || b.Student.NationalID.Contains(search));

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)PageSize);
        if (totalPages < 1) totalPages = 1;

        var restrictions = await query.OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(b => new MealRestrictionRowViewModel
            {
                ID = b.ID,
                StudentID = b.StudentID,
                StudentName = b.Student.FullName,
                NationalID = b.Student.NationalID,
                CityName = b.DormitoryCity.Name,
                FromDate = b.FromDate,
                ToDate = b.ToDate,
                MealType = b.MealType,
                Reason = b.Reason,
                IsActive = b.IsActive ?? false,
                CreatedByName = b.CreatedByNavigation != null ? b.CreatedByNavigation.Name : "",
                CreatedAt = b.CreatedAt
            }).ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var activeCount = await db.MealBlocks.CountAsync(b => b.IsActive == true && b.ToDate >= today);
        var expiredCount = await db.MealBlocks.CountAsync(b => b.IsActive == false || b.ToDate < today);

        return new MealRestrictionIndexViewModel
        {
            Tab = tab ?? "active",
            CityId = cityId,
            MealType = mealType,
            Search = search,
            Page = page,
            TotalPages = totalPages,
            ActiveCount = activeCount,
            ExpiredCount = expiredCount,
            TotalCount = total,
            Restrictions = restrictions,
            Cities = cities,
            MealTypes = mealTypes
        };
    }

    public async Task<(bool success, string message)> CreateRestrictionAsync(CreateRestrictionViewModel model, int userId)
    {
        var student = await db.Students.FindAsync(model.StudentID);
        if (student == null)
            return (false, "الطالب غير موجود");

        var toDate = model.ToDate ?? DateOnly.FromDateTime(DateTime.Today.AddYears(10));

        var block = new MealBlock
        {
            StudentID = model.StudentID,
            DormitoryCityID = model.DormitoryCityID,
            FromDate = model.FromDate,
            ToDate = toDate,
            MealType = model.MealType,
            Reason = model.Reason,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        db.MealBlocks.Add(block);
        await db.SaveChangesAsync();

        await audit.LogAsync(userId, "Staff", "MealRestriction.Create", "MealBlock",
            block.ID, null, new { model.StudentID, model.DormitoryCityID, model.FromDate, toDate, model.MealType, model.Reason });

        return (true, "تم حجب الوجبات بنجاح");
    }

    public async Task<(bool success, string message)> RemoveRestrictionAsync(int id, int userId)
    {
        var block = await db.MealBlocks.FindAsync(id);
        if (block == null)
            return (false, "الحجب غير موجود");

        block.IsActive = false;
        await db.SaveChangesAsync();

        await audit.LogAsync(userId, "Staff", "MealRestriction.Remove", "MealBlock",
            id, null, new { block.StudentID, block.FromDate, block.ToDate });

        return (true, "تم إزالة الحجب بنجاح");
    }

    public async Task<(bool success, string message)> RemoveExpiredRestrictionsAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expired = await db.MealBlocks.Where(b => b.IsActive == true && b.ToDate < today).ToListAsync();

        if (!expired.Any())
            return (false, "لا توجد حجوزات منتهية");

        foreach (var b in expired)
            b.IsActive = false;

        await db.SaveChangesAsync();

        await audit.LogAsync(userId, "Staff", "MealRestriction.RemoveExpired", "MealBlock",
            null, null, new { Count = expired.Count });

        return (true, $"تم إزالة {expired.Count} حجب منتهي");
    }
}
