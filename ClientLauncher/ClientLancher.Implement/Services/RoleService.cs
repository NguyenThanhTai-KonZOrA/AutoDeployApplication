using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.UnitOfWork;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;

namespace ClientLauncher.Implement.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            IRoleRepository roleRepository,
            IRolePermissionRepository rolePermissionRepository,
            IPermissionRepository permissionRepository,
            IUnitOfWork unitOfWork,
            ILogger<RoleService> logger)
        {
            _roleRepository = roleRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _permissionRepository = permissionRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<RoleResponse>> GetAllRolesAsync()
        {
            var roles = await _roleRepository.GetAllRolesWithPermissionsAsync();
            return roles.Select(r => new RoleResponse
            {
                Id = r.Id,
                RoleName = r.RoleName,
                Description = r.Description,
                IsActive = r.IsActive,
                Permissions = r.RolePermissions?.Select(rp => new PermissionResponse
                {
                    Id = rp.Permission!.Id,
                    PermissionName = rp.Permission.PermissionName,
                    PermissionCode = rp.Permission.PermissionCode,
                    Description = rp.Permission.Description,
                    Category = rp.Permission.Category,
                    IsActive = rp.Permission.IsActive,
                    CreatedAt = rp.Permission.CreatedAt
                }).ToList(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList();
        }

        public async Task<RoleResponse?> GetRoleByIdAsync(int roleId)
        {
            var role = await _roleRepository.GetRoleWithPermissionsAsync(roleId);
            if (role == null) return null;

            return new RoleResponse
            {
                Id = role.Id,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                Permissions = role.RolePermissions?.Select(rp => new PermissionResponse
                {
                    Id = rp.Permission!.Id,
                    PermissionName = rp.Permission.PermissionName,
                    PermissionCode = rp.Permission.PermissionCode,
                    Description = rp.Permission.Description,
                    Category = rp.Permission.Category,
                    IsActive = rp.Permission.IsActive,
                    CreatedAt = rp.Permission.CreatedAt
                }).ToList(),
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };
        }

        public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, string createdBy)
        {
            if (await _roleRepository.RoleNameExistsAsync(request.RoleName))
            {
                throw new InvalidOperationException($"Role name '{request.RoleName}' already exists.");
            }

            var role = new Role
            {
                RoleName = request.RoleName,
                Description = request.Description,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDelete = false
            };

            await _roleRepository.AddAsync(role);
            await _unitOfWork.SaveChangesAsync();

            if (request.PermissionIds != null && request.PermissionIds.Any())
            {
                var rolePermissions = request.PermissionIds.Select(permId => new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId,
                    CreatedBy = createdBy,
                    UpdatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsDelete = false
                }).ToList();

                await _rolePermissionRepository.AddRangeAsync(rolePermissions);
                await _unitOfWork.SaveChangesAsync();
            }

            return (await GetRoleByIdAsync(role.Id))!;
        }

        public async Task<RoleResponse> UpdateRoleAsync(UpdateRoleRequest request, string updatedBy)
        {
            var role = await _roleRepository.GetByIdAsync(request.Id);
            if (role == null)
            {
                throw new InvalidOperationException($"Role with ID {request.Id} not found.");
            }

            if (await _roleRepository.RoleNameExistsAsync(request.RoleName, request.Id))
            {
                throw new InvalidOperationException($"Role name '{request.RoleName}' already exists.");
            }

            role.RoleName = request.RoleName;
            role.Description = request.Description;
            role.UpdatedBy = updatedBy;
            role.UpdatedAt = DateTime.UtcNow;

            _roleRepository.Update(role);

            // Update permissions
            await _rolePermissionRepository.RemoveByRoleIdAsync(role.Id);

            if (request.PermissionIds != null && request.PermissionIds.Any())
            {
                var rolePermissions = request.PermissionIds.Select(permId => new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId,
                    CreatedBy = updatedBy,
                    UpdatedBy = updatedBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsDelete = false
                }).ToList();

                await _rolePermissionRepository.AddRangeAsync(rolePermissions);
            }

            await _unitOfWork.SaveChangesAsync();
            return (await GetRoleByIdAsync(role.Id))!;
        }

        public async Task<bool> DeleteRoleAsync(int roleId, string deletedBy)
        {
            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                throw new InvalidOperationException($"Role with ID {roleId} not found.");
            }

            role.IsDelete = true;
            role.UpdatedBy = deletedBy;
            role.UpdatedAt = DateTime.UtcNow;

            _roleRepository.Update(role);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int roleId, string updatedBy)
        {
            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                throw new InvalidOperationException($"Role with ID {roleId} not found.");
            }

            role.IsActive = !role.IsActive;
            role.UpdatedBy = updatedBy;
            role.UpdatedAt = DateTime.UtcNow;

            _roleRepository.Update(role);
            await _unitOfWork.SaveChangesAsync();
            return role.IsActive;
        }

        public async Task<List<RoleResponse>> GetActiveRolesByIdsAsync(List<int> roleIds)
        {
            var roles = await _roleRepository.GetAllRolesWithPermissionsAsync();
            var activeRoles = roles.Where(r => r.IsActive && roleIds.Contains(r.Id)).ToList();
            return activeRoles.Select(r => new RoleResponse
            {
                Id = r.Id,
                RoleName = r.RoleName,
                Description = r.Description,
                IsActive = r.IsActive,
                Permissions = r.RolePermissions?.Select(rp => new PermissionResponse
                {
                    Id = rp.Permission!.Id,
                    PermissionName = rp.Permission.PermissionName,
                    PermissionCode = rp.Permission.PermissionCode,
                    Description = rp.Permission.Description,
                    Category = rp.Permission.Category,
                    IsActive = rp.Permission.IsActive,
                    CreatedAt = rp.Permission.CreatedAt
                }).ToList(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList();
        }
    }
}