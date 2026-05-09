namespace UniStay.Services.Interfaces
{
    public interface IMealService
    {
        Task GenerateDailyMealsAsync(int dormitoryCityId, DateTime date);
        Task CancelBulkMealsAsync(int dormitoryCityId, DateTime from, DateTime to, string reason);
        Task<bool> CanConsumeAsync(int studentId, int dormitoryCityId, DateTime date);
    }
}
