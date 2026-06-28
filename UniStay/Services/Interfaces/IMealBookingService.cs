using UniStay.Models;
using UniStay.ViewModels.Meal;

namespace UniStay.Services.Interfaces;

public interface IMealBookingService
{
    Task<ScanBookingResultViewModel?> ScanStudentAsync(string searchTerm);
    Task<(bool success, string message)> BookMealAsync(BookMealViewModel model, int userId);
    Task<(int successCount, List<string> errors)> BookDatesAsync(BookDatesViewModel model, int userId);
    Task<List<DateOnly>> GetBookedDatesAsync(int studentId);

    Task<int> GetBookedCountInMonthAsync(int studentId, int month, int year);
    Task<List<DateOnly>> GetBookedDaysInMonthAsync(int studentId, int month, int year);
    Task<(bool canBook, string message)> CanBookDateAsync(int studentId, DateOnly date);
    Task<(bool success, string message)> BookDateAsync(int studentId, DateOnly date, int dormitoryCityId);
    Task<(bool success, string message)> UnbookDateAsync(int studentId, DateOnly date);
    Task<bool> IsMonthPaidAsync(int studentId, int month, int year);
    Task<(string currentMonthYear, string nextMonthYear)> GetBookingMonthsAsync(int studentId);
    Task<List<Meal>> GetBookedMealsAsync(int studentId);
    Task<string> GetDeadlineDisplayAsync();
}
