using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Meal;

namespace UniStay.Services.Implementations;

public class MealReceivingService(AssuitDbContext db, IAuditService audit, IMealService mealService) : IMealReceivingService
{
    public async Task<ScanResultViewModel?> ScanStudentAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return null;

        var student = await db.Students
            .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                .ThenInclude(a => a.CityRoom).ThenInclude(r => r.CityBuilding).ThenInclude(b => b.DormitoryCity)
            .FirstOrDefaultAsync(s => s.NationalID == searchTerm || s.StudentCode == searchTerm || s.ID.ToString() == searchTerm);

        if (student == null)
            return new ScanResultViewModel
            {
                IsEligible = false,
                EligibilityMessage = "الطالب غير موجود"
            };

        var allocation = student.Allocations.FirstOrDefault();
        var cityName = allocation?.CityRoom?.CityBuilding?.DormitoryCity?.Name ?? "";

        var today = DateOnly.FromDateTime(DateTime.Today);
        var cityId = allocation?.CityRoom?.CityBuilding?.DormitoryCityID ?? 0;

        var hasRestriction = await db.MealBlocks.AnyAsync(b =>
            b.StudentID == student.ID &&
            b.IsActive == true &&
            today >= b.FromDate && today <= b.ToDate);

        var restriction = hasRestriction
            ? await db.MealBlocks.FirstOrDefaultAsync(b =>
                b.StudentID == student.ID && b.IsActive == true &&
                today >= b.FromDate && today <= b.ToDate)
            : null;

        var availableMeals = await db.Meals
            .Where(m => m.StudentID == student.ID && m.MealDate == today
                && m.IsBooked == true && m.IsConsumed != true && m.IsActive == true)
            .Select(m => new EligibleMealViewModel
            {
                MealID = m.ID,
                MealType = m.MealType,
                MealTypeDisplay = m.MealType,
                MealDate = m.MealDate,
                Price = m.Price
            }).ToListAsync();

        return new ScanResultViewModel
        {
            StudentID = student.ID,
            StudentName = student.FullName,
            NationalID = student.NationalID,
            Photo = student.Photo,
            CityName = cityName,
            IsEligible = !hasRestriction && availableMeals.Any(),
            EligibilityMessage = hasRestriction
                ? "الطالب محظور من الوجبات"
                : !availableMeals.Any()
                    ? "لا توجد وجبات متاحة للاستلام اليوم"
                    : "يمكن استلام الوجبة",
            HasActiveRestriction = hasRestriction,
            RestrictionReason = restriction?.Reason,
            AvailableMeals = availableMeals
        };
    }

    public async Task<(bool success, string message)> ConfirmReceiptAsync(ConfirmReceiptViewModel model, int userId)
    {
        var meal = await db.Meals.Include(m => m.Student)
            .FirstOrDefaultAsync(m => m.ID == model.MealID);

        if (meal == null)
            return (false, "الوجبة غير موجودة");

        if (meal.IsConsumed == true)
            return (false, "الوجبة مستهلكة بالفعل");

        if (meal.StudentID != model.StudentID)
            return (false, "الوجبة لا تخص هذا الطالب");

        var canConsume = await mealService.CanConsumeAsync(model.StudentID, meal.DormitoryCityID, DateTime.Today);
        if (!canConsume)
            return (false, "لا يمكن استهلاك الوجبة (الطالب محظور)");

        var consumption = new MealConsumption
        {
            StudentID = model.StudentID,
            MealID = model.MealID,
            DormitoryCityID = meal.DormitoryCityID,
            MealDate = meal.MealDate,
            ScanMethod = model.ScanMethod,
            ConsumedAt = DateTime.UtcNow,
            RecordedBy = userId
        };

        meal.IsConsumed = true;
        db.MealConsumptions.Add(consumption);
        await db.SaveChangesAsync();

        await audit.LogAsync(userId, "Staff", "MealReceiving.Confirm", "Meal",
            meal.ID, null, new { model.StudentID, meal.MealDate, model.ScanMethod });

        return (true, "تم تسليم الوجبة بنجاح");
    }

    public async Task<ExcelImportResultViewModel> ImportFromExcelAsync(Stream excelStream, int cityId, int userId)
    {
        var result = new ExcelImportResultViewModel();
        var details = new List<ExcelImportRowViewModel>();

        using var workbook = new XLWorkbook(excelStream);
        var sheet = workbook.Worksheet(1);
        var rows = sheet.RangeUsed()?.RowsUsed();

        if (rows == null)
        {
            result.FailedCount = 1;
            result.Details.Add(new ExcelImportRowViewModel { RowNumber = 0, Status = "فشل", Message = "الملف فارغ" });
            return result;
        }

        var rowList = rows.Skip(1).ToList();
        result.TotalRows = rowList.Count;

        foreach (var row in rowList)
        {
            var rowNum = row.RowNumber();
            var detail = new ExcelImportRowViewModel { RowNumber = rowNum };

            try
            {
                var studentIdStr = row.Cell(1).GetString().Trim();
                var nationalId = row.Cell(2).GetString().Trim();
                var mealDateStr = row.Cell(3).GetString().Trim();
                var mealType = row.Cell(4).GetString().Trim();

                detail.StudentIDStr = studentIdStr;
                detail.NationalID = nationalId;
                detail.MealDate = mealDateStr;
                detail.MealType = mealType;

                Student? student = null;
                if (!string.IsNullOrEmpty(studentIdStr) && int.TryParse(studentIdStr, out var sid))
                    student = await db.Students.FindAsync(sid);
                if (student == null && !string.IsNullOrEmpty(nationalId))
                    student = await db.Students.FirstOrDefaultAsync(s => s.NationalID == nationalId);

                if (student == null)
                {
                    detail.Status = "فشل";
                    detail.Message = "الطالب غير موجود";
                    result.FailedCount++;
                    details.Add(detail);
                    continue;
                }

                if (!DateOnly.TryParse(mealDateStr, out var mealDate))
                {
                    detail.Status = "فشل";
                    detail.Message = "تاريخ غير صالح";
                    result.FailedCount++;
                    details.Add(detail);
                    continue;
                }

                var existing = await db.MealConsumptions.AnyAsync(mc =>
                    mc.StudentID == student.ID && mc.MealDate == mealDate && mc.Meal.MealType == mealType);

                if (existing)
                {
                    detail.Status = "مكرر";
                    detail.Message = "تم استلام الوجبة بالفعل";
                    result.DuplicateCount++;
                    details.Add(detail);
                    continue;
                }

                var meal = await db.Meals.FirstOrDefaultAsync(m =>
                    m.StudentID == student.ID && m.MealDate == mealDate &&
                    m.MealType == mealType && m.IsBooked == true && m.IsConsumed != true && m.IsActive == true);

                if (meal == null)
                {
                    detail.Status = "فشل";
                    detail.Message = "لا توجد وجبة نشطة بهذه البيانات";
                    result.FailedCount++;
                    details.Add(detail);
                    continue;
                }

                meal.IsConsumed = true;
                db.MealConsumptions.Add(new MealConsumption
                {
                    StudentID = student.ID,
                    MealID = meal.ID,
                    DormitoryCityID = cityId,
                    MealDate = mealDate,
                    ScanMethod = "Excel",
                    ConsumedAt = DateTime.UtcNow,
                    RecordedBy = userId
                });

                await db.SaveChangesAsync();

                detail.Status = "تم";
                detail.Message = "تم تسجيل الاستلام";
                result.ImportedCount++;
            }
            catch (Exception ex)
            {
                detail.Status = "خطأ";
                detail.Message = ex.Message;
                result.FailedCount++;
            }

            details.Add(detail);
        }

        result.Details = details;

        await audit.LogAsync(userId, "Staff", "MealReceiving.ExcelImport", "MealConsumption",
            null, null, new { cityId, Imported = result.ImportedCount, Failed = result.FailedCount, Duplicate = result.DuplicateCount });

        return result;
    }
}
