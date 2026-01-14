using Azure.Core;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.UnitOfWork;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.AspNetCore.Http;

namespace ClientLauncher.Implement.Services
{
    public class IconsService : IIconsService
    {
        private readonly IIconsRepository _iconsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _iconStoragePath;

        public IconsService(IIconsRepository iconsRepository, IUnitOfWork unitOfWork)
        {
            _iconsRepository = iconsRepository;
            _unitOfWork = unitOfWork;
            _iconStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "Icons");

            if (!Directory.Exists(_iconStoragePath))
            {
                Directory.CreateDirectory(_iconStoragePath);
            }
        }

        public async Task<IconResponse?> GetByIdAsync(int id)
        {
            var icon = await _iconsRepository.GetByIdAsync(id);
            return icon != null && !icon.IsDelete ? MapToDto(icon) : null;
        }

        public async Task<IEnumerable<IconResponse>> GetAllAsync()
        {
            var icons = await _iconsRepository.FindAsync(i => !i.IsDelete);
            return icons.Select(MapToDto);
        }

        public async Task<IEnumerable<IconResponse>> GetByTypeAsync(IconType type)
        {
            var icons = await _iconsRepository.GetByTypeAsync(type);
            return icons.Select(MapToDto);
        }

        public async Task<IconResponse?> GetByTypeAndReferenceIdAsync(IconType type, int referenceId)
        {
            var icon = await _iconsRepository.GetByTypeAndReferenceIdAsync(type, referenceId);
            return icon != null ? MapToDto(icon) : null;
        }

        public async Task<IconResponse> CreateAsync(CreateIconRequest request, string createdBy)
        {
            ValidateFile(request.File);

            var fileName = await SaveFileAsync(request.File);
            var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            var filePath = Path.Combine(_iconStoragePath, fileName);
            var fileUrl = $"/icons/{fileName}";

            var icon = new Icons
            {
                Name = request.Name,
                FilePath = filePath,
                FileUrl = fileUrl,
                FileExtension = fileExtension,
                FileSize = request.File.Length,
                Type = request.Type,
                ReferenceId = request.ReferenceId,
                IsActive = true,
                IsDelete = false,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = createdBy,
                UpdatedAt = DateTime.UtcNow
            };

            await _iconsRepository.AddAsync(icon);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(icon);
        }

        public async Task<IconResponse?> UpdateAsync(int id, UpdateIconRequest request, string updatedBy)
        {
            var icon = await _iconsRepository.GetByIdAsync(id);
            if (icon == null || icon.IsDelete)
                return null;

            if (!string.IsNullOrWhiteSpace(request.Name))
                icon.Name = request.Name;

            if (request.Type.HasValue)
                icon.Type = request.Type.Value;

            if (request.ReferenceId.HasValue)
                icon.ReferenceId = request.ReferenceId.Value;

            if (request.IsActive.HasValue)
                icon.IsActive = request.IsActive.Value;

            var fileName = icon.Name;
            var fileExtension = icon.FileExtension;
            var filePath = icon.FilePath;
            var fileUrl = icon.FileUrl;

            if (request.File != null)
            {
                ValidateFile(request.File);
                fileName = await SaveFileAsync(request.File);
                fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                filePath = Path.Combine(_iconStoragePath, fileName);
                fileUrl = $"/icons/{fileName}";
            }

            icon.UpdatedBy = updatedBy;
            icon.UpdatedAt = DateTime.UtcNow;
            icon.Name = fileName;
            icon.FileExtension = fileExtension;
            icon.FilePath = filePath;
            icon.FileUrl = fileUrl;
            _iconsRepository.Update(icon);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(icon);
        }

        public async Task<IconResponse?> UploadIconAsync(int id, IFormFile file, string updatedBy)
        {
            var icon = await _iconsRepository.GetByIdAsync(id);
            if (icon == null || icon.IsDelete)
                return null;

            ValidateFile(file);

            // Delete old file
            if (File.Exists(icon.FilePath))
            {
                File.Delete(icon.FilePath);
            }

            // Save new file
            var fileName = await SaveFileAsync(file);
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var filePath = Path.Combine(_iconStoragePath, fileName);
            var fileUrl = $"/icons/{fileName}";

            icon.FilePath = filePath;
            icon.FileUrl = fileUrl;
            icon.FileExtension = fileExtension;
            icon.FileSize = file.Length;
            icon.UpdatedBy = updatedBy;
            icon.UpdatedAt = DateTime.UtcNow;

            _iconsRepository.Update(icon);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(icon);
        }

        public async Task<bool> DeleteAsync(int id, string deletedBy)
        {
            var icon = await _iconsRepository.GetByIdAsync(id);
            if (icon == null || icon.IsDelete)
                return false;

            icon.IsDelete = true;
            icon.IsActive = false;
            icon.UpdatedBy = deletedBy;
            icon.UpdatedAt = DateTime.UtcNow;

            _iconsRepository.Update(icon);
            await _unitOfWork.SaveChangesAsync();

            // Optionally delete physical file
            if (File.Exists(icon.FilePath))
            {
                File.Delete(icon.FilePath);
            }

            return true;
        }

        private void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg", ".ico", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException($"Only {string.Join(", ", allowedExtensions)} files are allowed");

            if (file.Length > 5 * 1024 * 1024) // 5MB
                throw new ArgumentException("File size cannot exceed 5MB");
        }

        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(_iconStoragePath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        private IconResponse MapToDto(Icons icon)
        {
            return new IconResponse
            {
                Id = icon.Id,
                Name = icon.Name,
                FileUrl = icon.FileUrl,
                FilePath = icon.FilePath,
                FileExtension = icon.FileExtension,
                FileSize = icon.FileSize,
                Type = icon.Type,
                ReferenceId = icon.ReferenceId,
                CreatedAt = icon.CreatedAt,
                UpdatedAt = icon.UpdatedAt,
                IsActive = icon.IsActive
            };
        }
    }
}