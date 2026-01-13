using ClientLauncher.Implement.ApplicationDbContext;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLauncher.Implement.Repositories
{
    public class EmployeeRoleRepository : GenericRepository<EmployeeRole>, IEmployeeRoleRepository
    {
        public EmployeeRoleRepository(DeploymentManagerDbContext context) : base(context)
        {
        }

        public async Task<List<EmployeeRole>> GetByEmployeeIdAsync(int employeeId)
        {
            return await _context.EmployeeRoles
                .Include(er => er.Role)
                .Where(er => er.EmployeeId == employeeId)
                .ToListAsync();
        }

        public async Task RemoveByEmployeeIdAsync(int employeeId)
        {
            var employeeRoles = await _context.EmployeeRoles
                .Where(er => er.EmployeeId == employeeId)
                .ToListAsync();
            _context.EmployeeRoles.RemoveRange(employeeRoles);
        }

        public async Task<List<int>> GetRoleIdsByEmployeeIdAsync(int employeeId)
        {
            return await _context.EmployeeRoles
                .Where(er => er.EmployeeId == employeeId)
                .Select(er => er.RoleId)
                .ToListAsync();
        }

        public async Task<List<string>> GetPermissionsByEmployeeIdAsync(int employeeId)
        {
            return await _context.EmployeeRoles
                .Where(er => er.EmployeeId == employeeId)
                .SelectMany(er => er.Role!.RolePermissions!)
                .Select(rp => rp.Permission!.PermissionCode!)
                .Distinct()
                .ToListAsync();
        }
    }
}
