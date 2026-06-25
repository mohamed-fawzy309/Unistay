using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;

namespace UniStay.Services.Helpers;

public static class StudentLookupHelper
{
    public static async Task<Student?> FindStudentWithAllocationAsync(AssuitDbContext db, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return null;

        return await db.Students
            .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                .ThenInclude(a => a.CityRoom).ThenInclude(r => r.CityBuilding).ThenInclude(b => b.DormitoryCity)
            .FirstOrDefaultAsync(s => s.NationalID == searchTerm || s.StudentCode == searchTerm || s.ID.ToString() == searchTerm);
    }

    public static string GetStudentCityName(Student student)
    {
        var allocation = student.Allocations.FirstOrDefault();
        return allocation?.CityRoom?.CityBuilding?.DormitoryCity?.Name ?? "";
    }

    public static async Task<(bool hasRestriction, MealBlock? restriction)> GetActiveRestrictionAsync(AssuitDbContext db, int studentId, DateOnly date)
    {
        var restriction = await db.MealBlocks.FirstOrDefaultAsync(b =>
            b.StudentID == studentId && b.IsActive == true &&
            date >= b.FromDate && date <= b.ToDate);

        return (restriction != null, restriction);
    }

    public static async Task<Student?> FindStudentByIdOrNationalIdAsync(AssuitDbContext db, string? studentIdStr, string? nationalId)
    {
        Student? student = null;
        if (!string.IsNullOrEmpty(studentIdStr) && int.TryParse(studentIdStr, out var sid))
            student = await db.Students.FindAsync(sid);
        if (student == null && !string.IsNullOrEmpty(nationalId))
            student = await db.Students.FirstOrDefaultAsync(s => s.NationalID == nationalId);
        return student;
    }
}
