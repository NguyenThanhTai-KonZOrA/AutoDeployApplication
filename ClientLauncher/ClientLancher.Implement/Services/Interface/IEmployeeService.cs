using ClientLancher.Implement.EntityModels;

namespace ClientLancher.Implement.Services.Interface
{
    public interface IEmployeeService
    {
        Task<Employee> GetOrCreateEmployeeFromWindowsAccountAsync(string username);
        Task<Employee?> GetEmployeeByCodeAsync(string employeeCode);
    }
}