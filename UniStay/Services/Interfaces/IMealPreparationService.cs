using UniStay.ViewModels.Meal;

namespace UniStay.Services.Interfaces;

public interface IMealPreparationService
{
    Task<MealPreparationIndexViewModel> GetPreparationSummaryAsync(DateOnly? date, int? cityId);
    Task<DailyPreparationSheetViewModel> GetDailySheetAsync(DateOnly date, int? cityId);
    Task<KitchenReportViewModel> GetKitchenReportAsync(DateOnly date, int? cityId);
    Task<DistributionReportViewModel> GetDistributionReportAsync(DateOnly date, int? cityId);
    Task<byte[]> ExportDailySheetExcelAsync(DateOnly date, int? cityId);
    Task<byte[]> ExportDailySheetPdfAsync(DateOnly date, int? cityId);
}
