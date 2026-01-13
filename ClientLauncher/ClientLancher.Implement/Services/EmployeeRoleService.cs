using ClientLauncher.Implement.ApplicationDbContext;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.UnitOfWork;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClientLauncher.Implement.Services
{
    public class EmployeeRoleService : IEmployeeRoleService
    {
        private readonly IEmployeeRoleRepository _employeeRoleRepository;
        private readonly DeploymentManagerDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EmployeeRoleService> _logger;

        public EmployeeRoleService(
            IEmployeeRoleRepository employeeRoleRepository,
            DeploymentManagerDbContext context,
            IUnitOfWork unitOfWork,
            ILogger<EmployeeRoleService> logger)
        {
            _employeeRoleRepository = employeeRoleRepository;
            _context = context;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<EmployeeWithRolesResponse?> GetEmployeeWithRolesAsync(int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                        .ThenInclude(r => r!.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null) return null;

            var permissions = employee.EmployeeRoles?
                .SelectMany(er => er.Role!.RolePermissions!)
                .Select(rp => rp.Permission!.PermissionCode!)
                .Distinct()
                .ToList();

            return new EmployeeWithRolesResponse
            {
                Id = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                FullName = employee.FullName,
                Email = employee.Email,
                Department = employee.Department,
                Position = employee.Position,
                Roles = employee.EmployeeRoles?.Select(er => new RoleResponse
                {
                    Id = er.Role!.Id,
                    RoleName = er.Role.RoleName,
                    Description = er.Role.Description,
                    IsActive = er.Role.IsActive,
                    Permissions = er.Role.RolePermissions?.Select(rp => new PermissionResponse
                    {
                        Id = rp.Permission!.Id,
                        PermissionName = rp.Permission.PermissionName,
                        PermissionCode = rp.Permission.PermissionCode,
                        Description = rp.Permission.Description,
                        Category = rp.Permission.Category,
                        IsActive = rp.Permission.IsActive,
                        CreatedAt = rp.Permission.CreatedAt
                    }).ToList()
                }).ToList(),
                Permissions = permissions
            };
        }

        public async Task<bool> AssignRolesToEmployeeAsync(AssignRoleToEmployeeRequest request, string assignedBy)
        {
            var employee = await _context.Employees.FindAsync(request.EmployeeId);
            if (employee == null)
            {
                throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found.");
            }

            await _employeeRoleRepository.RemoveByEmployeeIdAsync(request.EmployeeId);

            var employeeRoles = request.RoleIds.Select(roleId => new EmployeeRole
            {
                EmployeeId = request.EmployeeId,
                RoleId = roleId,
                CreatedBy = assignedBy,
                UpdatedBy = assignedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDelete = false
            }).ToList();

            await _employeeRoleRepository.AddRangeAsync(employeeRoles);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Assigned {Count} roles to Employee {EmployeeId} by {AssignedBy}",
                request.RoleIds.Count, request.EmployeeId, assignedBy);

            return true;
        }

        public async Task<List<string>> GetEmployeePermissionsAsync(int employeeId)
        {
            return await _employeeRoleRepository.GetPermissionsByEmployeeIdAsync(employeeId);
        }
    }
}