using ClientLauncher.Implement.ApplicationDbContext;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLauncher.Implement.Repositories
{
    public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
    {
        private readonly DeploymentManagerDbContext _context;
        public EmployeeRepository(DeploymentManagerDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Employee?> GetByEmployeeByCodeOrUserNameAsync(string employeeCode)
        {
            return await _context.Employees
                .Where(e => (e.EmployeeCode == employeeCode || e.WindowAccount == employeeCode) && e.IsActive).Include(x => x.EmployeeRoles)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Employee>> GetActiveEmployeesAsync()
        {
            return await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FullName)
                .ToListAsync();
        }
    }
}
