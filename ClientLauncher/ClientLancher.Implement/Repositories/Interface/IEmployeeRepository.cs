using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.Repositories.Interface
{
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        Task<Employee?> GetByEmployeeByCodeOrUserNameAsync(string employeeCode);
        Task<List<Employee>> GetActiveEmployeesAsync();
    }
}