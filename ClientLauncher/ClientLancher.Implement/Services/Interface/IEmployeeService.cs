using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IEmployeeService
    {
        Task<Employee> GetOrCreateEmployeeFromWindowsAccountAsync(string username);
        Task<Employee?> GetEmployeeByCodeAsync(string employeeCode);
    }
}