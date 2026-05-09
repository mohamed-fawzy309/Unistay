namespace UniStay.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(
            int userId,
            string userType,
            string action,
            string tableName,
            int? recordId = null,
            object? oldValues = null,
            object? newValues = null,
            string? ipAddress = null,
            int? dormitoryCityId = null);
    }
}