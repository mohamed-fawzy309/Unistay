// Helpers/AuditHelper.cs
using UniStay.Services.Interfaces;

namespace UniStay.Helpers
{
    public static class AuditHelper
    {
        public static async Task LogAsync(
    IAuditService auditService,
    HttpContext httpContext,
    string action,
    string tableName,
    int? recordId = null,
    object? oldValues = null,
    object? newValues = null)
        {
            var userIdClaim = httpContext.User.FindFirst("UserID")?.Value;
            int userId = int.TryParse(userIdClaim, out int id) ? id : 0;

            string userType = httpContext.Items.ContainsKey("CurrentStudent") ? "Student" : "Staff";

            string? ip = httpContext.Connection.RemoteIpAddress?.ToString();

            await auditService.LogAsync(
                userId,
                userType,
                action,
                tableName,
                recordId ?? 0,   // 🔥 FIX
                oldValues,
                newValues,
                ip
            );
        }
    }
}