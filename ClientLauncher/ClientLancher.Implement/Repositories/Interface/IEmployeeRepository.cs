using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.Repositories.Interface
{
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        Task<Employee?> GetEmployeeByCodeOrUserNameAsync(string employeeCode);
        Task<List<Employee>> GetActiveEmployeesAsync();
        Task<Employee?> GetEmployeeByEmailAsync(string email);
    }
}