using UniStay.ViewModels.Meal;

namespace UniStay.Services.Interfaces;

public interface IMealReceivingService
{
    Task<ScanResultViewModel?> ScanStudentAsync(string searchTerm);
    Task<(bool success, string message)> ConfirmReceiptAsync(ConfirmReceiptViewModel model, int userId);
    Task<ExcelImportResultViewModel> ImportFromExcelAsync(Stream excelStream, int cityId, int userId);
}
