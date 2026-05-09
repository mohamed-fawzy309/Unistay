using Newtonsoft.Json;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations
{
    public class AuditService : IAuditService
    {
        private readonly AssuitDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(AssuitDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            int userId,
            string userType,
            string action,
            string tableName,
            int? recordId = null,
            object? oldValues = null,
            object? newValues = null,
            string? ipAddress = null,
            int? dormitoryCityId = null)
        {
            var log = new AuditLog
            {
                UserID = userId,
                UserType = userType,
                Action = action,
                TableName = tableName,
                RecordID = recordId,
                OldValues = oldValues != null ? JsonConvert.SerializeObject(oldValues) : null,
                NewValues = newValues != null ? JsonConvert.SerializeObject(newValues) : null,
                IPAddress = ipAddress ?? _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                DormitoryCityID = dormitoryCityId,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}