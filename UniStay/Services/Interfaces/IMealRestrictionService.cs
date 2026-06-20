using UniStay.ViewModels.Meal;

namespace UniStay.Services.Interfaces;

public interface IMealRestrictionService
{
    Task<MealRestrictionIndexViewModel> GetRestrictionsAsync(string? tab, int? cityId, string? mealType, string? search, int page);
    Task<(bool success, string message)> CreateRestrictionAsync(CreateRestrictionViewModel model, int userId);
    Task<(bool success, string message)> RemoveRestrictionAsync(int id, int userId);
    Task<(bool success, string message)> RemoveExpiredRestrictionsAsync(int userId);
}
