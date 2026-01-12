using ClientLancher.Implement.ApplicationDbContext;
using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLancher.Implement.Repositories
{
    public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
    {
        private readonly ClientLancherDbContext _context;
        public EmployeeRepository(ClientLancherDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Employee?> GetEmployeeByCodeOrUserNameAsync(string employeeCode)
        {
            return await _context.Employees
                .Where(e => (e.EmployeeCode == employeeCode || e.WindowAccount == employeeCode) && e.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            return await _context.Employees
                .Where(e => e.Email == email && e.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Employee>> GetActiveEmployeesAsync()
        {
            return await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.Id)
                .ToListAsync();
        }
    }
}
