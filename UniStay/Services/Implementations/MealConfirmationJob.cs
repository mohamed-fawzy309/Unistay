using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations;

public class MealConfirmationJob
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MealConfirmationJob(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AssuitDbContext>();
        var mealService = scope.ServiceProvider.GetRequiredService<IMealService>();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var cities = await db.DormitoryCities
            .Where(c => c.IsActive)
            .Select(c => new { c.ID, c.Name })
            .ToListAsync();

        var today = DateTime.Today;

        foreach (var city in cities)
        {
            await mealService.GenerateDailyMealsAsync(city.ID, today);
            await audit.LogAsync(0, "System", "Meal.AutoConfirm", "Meal",
                null, null, new { cityId = city.ID, cityName = city.Name, Date = today.ToString("yyyy-MM-dd") });
        }
    }
}
