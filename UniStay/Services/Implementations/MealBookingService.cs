using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Meal;

namespace UniStay.Services.Implementations;

public class MealBookingService(AssuitDbContext db, IAuditService audit) : IMealBookingService
{
    public async Task<ScanBookingResultViewModel?> ScanStudentAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return null;

        var student = await db.Students
            .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                .ThenInclude(a => a.CityRoom).ThenInclude(r => r.CityBuilding).ThenInclude(b => b.DormitoryCity)
            .FirstOrDefaultAsync(s => s.NationalID == searchTerm || s.StudentCode == searchTerm || s.ID.ToString() == searchTerm);

        if (student == null)
            return new ScanBookingResultViewModel
            {
                IsEligible = false,
                EligibilityMessage = "الطالب غير موجود"
            };

        var allocation = student.Allocations.FirstOrDefault();
        var cityName = allocation?.CityRoom?.CityBuilding?.DormitoryCity?.Name ?? "";

        var today = DateOnly.FromDateTime(DateTime.Today);
        var hasRestriction = await db.MealBlocks.AnyAsync(b =>
            b.StudentID == student.ID && b.IsActive == true &&
            today >= b.FromDate && today <= b.ToDate);

        var restriction = hasRestriction
            ? await db.MealBlocks.FirstOrDefaultAsync(b =>
                b.StudentID == student.ID && b.IsActive == true &&
                today >= b.FromDate && today <= b.ToDate)
            : null;

        return new ScanBookingResultViewModel
        {
            StudentID = student.ID,
            StudentName = student.FullName,
            NationalID = student.NationalID,
            CityName = cityName,
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

        var hasRestriction = await db.MealBlocks.AnyAsync(b =>
            b.StudentID == model.StudentID && b.IsActive == true &&
            model.MealDate >= b.FromDate && model.MealDate <= b.ToDate &&
            (b.MealType == null || b.MealType == model.MealType));

        if (hasRestriction)
            return (false, "الطالب محظور من حجز هذه الوجبة");

        var existing = await db.Meals.AnyAsync(m =>
            m.StudentID == model.StudentID && m.MealDate == model.MealDate &&
            m.MealType == model.MealType && m.IsBooked == true);

        if (existing)
            return (false, "الوجبة محجوزة بالفعل لهذا اليوم");

        var price = model.MealType == "Lunch" ? 15m : model.MealType == "Dinner" ? 10m : 12m;

        var meal = new Meal
        {
            StudentID = model.StudentID,
            DormitoryCityID = model.DormitoryCityID,
            MealDate = model.MealDate,
            MealType = model.MealType,
            Price = price,
            IsBooked = true,
            IsConsumed = false,
            IsActive = true
        };

        db.Meals.Add(meal);
        await db.SaveChangesAsync();

        await audit.LogAsync(userId, "Staff", "MealBooking.Create", "Meal",
            meal.ID, null, new { model.StudentID, model.MealDate, model.MealType });

        return (true, "تم حجز الوجبة بنجاح");
    }

    public async Task<BookingExcelImportResultViewModel> ImportFromExcelAsync(Stream excelStream, int cityId, int userId)
    {
        var result = new BookingExcelImportResultViewModel();
        var details = new List<BookingExcelImportRowViewModel>();

        using var workbook = new XLWorkbook(excelStream);
        var sheet = workbook.Worksheet(1);
        var rows = sheet.RangeUsed()?.RowsUsed();

        if (rows == null)
        {
            result.FailedCount = 1;
            result.Details.Add(new BookingExcelImportRowViewModel { RowNumber = 0, Status = "فشل", Message = "الملف فارغ" });
            return result;
        }

        var rowList = rows.Skip(1).ToList();
        result.TotalRows = rowList.Count;

        foreach (var row in rowList)
        {
            var rowNum = row.RowNumber();
            var detail = new BookingExcelImportRowViewModel { RowNumber = rowNum };

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

                var conflict = await db.Meals.AnyAsync(m =>
                    m.StudentID == student.ID && m.MealDate == mealDate &&
                    m.MealType == mealType && m.IsBooked == true);

                if (conflict)
                {
                    detail.Status = "مكرر";
                    detail.Message = "الوجبة محجوزة بالفعل";
                    result.DuplicateCount++;
                    details.Add(detail);
                    continue;
                }

                var hasRestriction = await db.MealBlocks.AnyAsync(b =>
                    b.StudentID == student.ID && b.IsActive == true &&
                    mealDate >= b.FromDate && mealDate <= b.ToDate &&
                    (b.MealType == null || b.MealType == mealType));

                if (hasRestriction)
                {
                    detail.Status = "فشل";
                    detail.Message = "الطالب محظور من الحجز";
                    result.FailedCount++;
                    details.Add(detail);
                    continue;
                }

                var price = mealType == "Lunch" ? 15m : mealType == "Dinner" ? 10m : 12m;

                db.Meals.Add(new Meal
                {
                    StudentID = student.ID,
                    DormitoryCityID = cityId,
                    MealDate = mealDate,
                    MealType = mealType,
                    Price = price,
                    IsBooked = true,
                    IsConsumed = false,
                    IsActive = true
                });

                await db.SaveChangesAsync();

                detail.Status = "تم";
                detail.Message = "تم حجز الوجبة";
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

        await audit.LogAsync(userId, "Staff", "MealBooking.ExcelImport", "Meal",
            null, null, new { cityId, Imported = result.ImportedCount, Failed = result.FailedCount, Duplicate = result.DuplicateCount });

        return result;
    }
}
