using UniStay.ViewModels.Meal;

namespace UniStay.Services.Interfaces;

public interface IMealBookingService
{
    Task<ScanBookingResultViewModel?> ScanStudentAsync(string searchTerm);
    Task<(bool success, string message)> BookMealAsync(BookMealViewModel model, int userId);
    Task<BookingExcelImportResultViewModel> ImportFromExcelAsync(Stream excelStream, int cityId, int userId);
}
