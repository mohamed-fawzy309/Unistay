using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations
{
    public class EmployeeApiService : IEmployeeApiService
    {
        private readonly AssuitDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public EmployeeApiService(AssuitDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private string? BaseUrl => _configuration["ExternalApis:EmployeeApi:BaseUrl"];
        private string? ApiKey => _configuration["ExternalApis:EmployeeApi:ApiKey"];

        public async Task<EmployeeRecord?> LookupByCodeAsync(string employeeCode)
        {
            var local = await _db.EmployeeRecords
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode && e.IsActive == true);
            if (local != null) return local;

            return await FetchFromApi($"employees/by-code/{employeeCode}");
        }

        public async Task<EmployeeRecord?> LookupByNationalIdAsync(string nationalId)
        {
            var local = await _db.EmployeeRecords
                .FirstOrDefaultAsync(e => e.NationalID == nationalId && e.IsActive == true);
            if (local != null) return local;

            return await FetchFromApi($"employees/by-national-id/{nationalId}");
        }

        public async Task<List<EmployeeRecord>> SyncEmployeesAsync()
        {
            if (string.IsNullOrEmpty(BaseUrl))
                return await _db.EmployeeRecords.Where(e => e.IsActive == true).ToListAsync();

            try
            {
                var client = _httpClientFactory.CreateClient("EmployeeApi");
                var records = await client.GetFromJsonAsync<List<EmployeeApiDto>>("employees/all");

                if (records == null || records.Count == 0)
                    return await _db.EmployeeRecords.Where(e => e.IsActive == true).ToListAsync();

                foreach (var dto in records)
                {
                    var existing = await _db.EmployeeRecords
                        .FirstOrDefaultAsync(e => e.EmployeeCode == dto.EmployeeCode);

                    if (existing != null)
                    {
                        existing.FullName = dto.FullName;
                        existing.NationalID = dto.NationalID;
                        existing.Email = dto.Email;
                        existing.Phone = dto.Phone;
                        existing.JobTitle = dto.JobTitle;
                        existing.Department = dto.Department;
                        existing.IsActive = dto.IsActive;
                        existing.LastSyncedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _db.EmployeeRecords.Add(new EmployeeRecord
                        {
                            EmployeeCode = dto.EmployeeCode,
                            FullName = dto.FullName,
                            NationalID = dto.NationalID,
                            Email = dto.Email,
                            Phone = dto.Phone,
                            JobTitle = dto.JobTitle,
                            Department = dto.Department,
                            IsActive = dto.IsActive,
                            LastSyncedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _db.SaveChangesAsync();
            }
            catch
            {
                // Fall back to local data on network failure
            }

            return await _db.EmployeeRecords.Where(e => e.IsActive == true).ToListAsync();
        }

        public async Task<bool> IsEmployeeActiveAsync(string employeeCode)
        {
            var record = await LookupByCodeAsync(employeeCode);
            return record?.IsActive == true;
        }

        private async Task<EmployeeRecord?> FetchFromApi(string endpoint)
        {
            if (string.IsNullOrEmpty(BaseUrl))
                return null;

            try
            {
                var client = _httpClientFactory.CreateClient("EmployeeApi");
                var dto = await client.GetFromJsonAsync<EmployeeApiDto>(endpoint);
                if (dto == null) return null;

                var existing = await _db.EmployeeRecords
                    .FirstOrDefaultAsync(e => e.EmployeeCode == dto.EmployeeCode);

                if (existing != null)
                {
                    existing.FullName = dto.FullName;
                    existing.NationalID = dto.NationalID;
                    existing.Email = dto.Email;
                    existing.Phone = dto.Phone;
                    existing.JobTitle = dto.JobTitle;
                    existing.Department = dto.Department;
                    existing.IsActive = dto.IsActive;
                    existing.LastSyncedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    return existing;
                }

                var record = new EmployeeRecord
                {
                    EmployeeCode = dto.EmployeeCode,
                    FullName = dto.FullName,
                    NationalID = dto.NationalID,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    JobTitle = dto.JobTitle,
                    Department = dto.Department,
                    IsActive = dto.IsActive,
                    LastSyncedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _db.EmployeeRecords.Add(record);
                await _db.SaveChangesAsync();
                return record;
            }
            catch
            {
                return null;
            }
        }
    }

    internal class EmployeeApiDto
    {
        public string EmployeeCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? NationalID { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public bool IsActive { get; set; }
    }
}
