using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IEmployeeService
    {
        Task<Employee> GetOrCreateEmployeeFromWindowsAccountAsync(string username);
        Task<Employee> GetOrCreateDefaultEmployeeAsync(string username);
        Task<Employee?> GetEmployeeByCodeAsync(string employeeCode);
        Task<bool> IsUserAdminAsync(string userName);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<List<Employee>> GetActiveEmployeesAsync();
    }
}