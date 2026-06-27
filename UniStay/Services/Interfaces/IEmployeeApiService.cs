using UniStay.Models;

namespace UniStay.Services.Interfaces
{
    public interface IEmployeeApiService
    {
        Task<EmployeeRecord?> LookupByCodeAsync(string employeeCode);
        Task<EmployeeRecord?> LookupByNationalIdAsync(string nationalId);
        Task<List<EmployeeRecord>> SyncEmployeesAsync();
        Task<bool> IsEmployeeActiveAsync(string employeeCode);
    }
}
