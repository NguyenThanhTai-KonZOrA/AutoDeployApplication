using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.UnitOfWork;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;

namespace ClientLauncher.Implement.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            IPermissionRepository permissionRepository,
            IUnitOfWork unitOfWork,
            ILogger<PermissionService> logger)
        {
            _permissionRepository = permissionRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<PermissionResponse>> GetAllPermissionsAsync()
        {
            var permissions = await _permissionRepository.GetAllAsync();
            return permissions.Select(p => new PermissionResponse
            {
                Id = p.Id,
                PermissionName = p.PermissionName,
                PermissionCode = p.PermissionCode,
                Description = p.Description,
                Category = p.Category,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();
        }

        public async Task<PermissionResponse?> GetPermissionByIdAsync(int permissionId)
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId);
            if (permission == null) return null;

            return new PermissionResponse
            {
                Id = permission.Id,
                PermissionName = permission.PermissionName,
                PermissionCode = permission.PermissionCode,
                Description = permission.Description,
                Category = permission.Category,
                IsActive = permission.IsActive,
                CreatedAt = permission.CreatedAt
            };
        }

        public async Task<PermissionResponse> CreatePermissionAsync(CreatePermissionRequest request, string createdBy)
        {
            if (await _permissionRepository.PermissionCodeExistsAsync(request.PermissionCode))
            {
                throw new InvalidOperationException($"Permission code '{request.PermissionCode}' already exists.");
            }

            var permission = new Permission
            {
                PermissionName = request.PermissionName,
                PermissionCode = request.PermissionCode,
                Description = request.Description,
                Category = request.Category,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDelete = false
            };

            await _permissionRepository.AddAsync(permission);
            await _unitOfWork.SaveChangesAsync();

            return (await GetPermissionByIdAsync(permission.Id))!;
        }

        public async Task<PermissionResponse> UpdatePermissionAsync(UpdatePermissionRequest request, string updatedBy)
        {
            var permission = await _permissionRepository.GetByIdAsync(request.Id);
            if (permission == null)
            {
                throw new InvalidOperationException($"Permission with ID {request.Id} not found.");
            }

            if (!string.IsNullOrEmpty(request.PermissionCode) &&
                await _permissionRepository.PermissionCodeExistsAsync(request.PermissionCode, request.Id))
            {
                throw new InvalidOperationException($"Permission code '{request.PermissionCode}' already exists.");
            }

            permission.PermissionName = request.PermissionName;
            permission.PermissionCode = request.PermissionCode;
            permission.Description = request.Description;
            permission.Category = request.Category;
            permission.UpdatedBy = updatedBy;
            permission.UpdatedAt = DateTime.UtcNow;

            _permissionRepository.Update(permission);
            await _unitOfWork.SaveChangesAsync();

            return (await GetPermissionByIdAsync(permission.Id))!;
        }

        public async Task<bool> DeletePermissionAsync(int permissionId, string deletedBy)
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId);
            if (permission == null)
            {
                throw new InvalidOperationException($"Permission with ID {permissionId} not found.");
            }

            permission.IsDelete = true;
            permission.UpdatedBy = deletedBy;
            permission.UpdatedAt = DateTime.UtcNow;

            _permissionRepository.Update(permission);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int permissionId, string updatedBy)
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId);
            if (permission == null)
            {
                throw new InvalidOperationException($"Permission with ID {permissionId} not found.");
            }

            permission.IsActive = !permission.IsActive;
            permission.UpdatedBy = updatedBy;
            permission.UpdatedAt = DateTime.UtcNow;

            _permissionRepository.Update(permission);
            await _unitOfWork.SaveChangesAsync();
            return permission.IsActive;
        }

        public async Task<Dictionary<string, List<PermissionResponse>>> GetPermissionsByCategoryAsync()
        {
            var permissions = await GetAllPermissionsAsync();
            return permissions
                .GroupBy(p => p.Category ?? "Other")
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );
        }
    }
}