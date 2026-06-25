using UniStay.ViewModels.Meal;

namespace UniStay.Services.Interfaces;

public interface IMealBookingService
{
    Task<ScanBookingResultViewModel?> ScanStudentAsync(string searchTerm);
    Task<(bool success, string message)> BookMealAsync(BookMealViewModel model, int userId);
    Task<(int successCount, List<string> errors)> BookDatesAsync(BookDatesViewModel model, int userId);
    Task<List<DateOnly>> GetBookedDatesAsync(int studentId);
}
