using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.UnitOfWork;
using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;

namespace ClientLancher.Implement.Services
{
    public class ApplicationManagementService : IApplicationManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ApplicationManagementService> _logger;

        public ApplicationManagementService(
            IUnitOfWork unitOfWork,
            ILogger<ApplicationManagementService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApplicationDetailResponse> CreateApplicationAsync(ApplicationCreateRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new application: {AppCode}", request.AppCode);

                // Validate AppCode is unique
                var existing = await _unitOfWork.Applications.GetByAppCodeAsync(request.AppCode);
                if (existing != null)
                {
                    throw new Exception($"Application with code '{request.AppCode}' already exists");
                }

                // Validate category if provided
                if (request.CategoryId.HasValue)
                {
                    var category = await _unitOfWork.ApplicationCategories.GetByIdAsync(request.CategoryId.Value);
                    if (category == null)
                    {
                        throw new Exception($"Category with ID {request.CategoryId} not found");
                    }
                }

                var application = new Application
                {
                    AppCode = request.AppCode,
                    Name = request.Name,
                    Description = request.Description,
                    IconUrl = request.IconUrl ?? string.Empty,
                    CategoryId = request.CategoryId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Applications.AddAsync(application);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Application created successfully: ID {Id}", application.Id);

                return await GetApplicationWithStatsAsync(application.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application");
                throw;
            }
        }

        public async Task<ApplicationDetailResponse> UpdateApplicationAsync(int id, ApplicationUpdateRequest request)
        {
            try
            {
                var application = await _unitOfWork.Applications.GetByIdAsync(id);
                if (application == null)
                {
                    throw new Exception($"Application with ID {id} not found");
                }

                _logger.LogInformation("Updating application: {AppCode}", application.AppCode);

                if (request.Name != null)
                    application.Name = request.Name;

                if (request.Description != null)
                    application.Description = request.Description;

                if (request.IconUrl != null)
                    application.IconUrl = request.IconUrl;

                if (request.CategoryId.HasValue)
                {
                    var category = await _unitOfWork.ApplicationCategories.GetByIdAsync(request.CategoryId.Value);
                    if (category == null)
                    {
                        throw new Exception($"Category with ID {request.CategoryId} not found");
                    }
                    application.CategoryId = request.CategoryId;
                }

                if (request.IsActive.HasValue)
                    application.IsActive = request.IsActive.Value;

                application.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Applications.Update(application);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Application updated successfully: ID {Id}", id);

                return await GetApplicationWithStatsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteApplicationAsync(int id)
        {
            try
            {
                var application = await _unitOfWork.Applications.GetByIdAsync(id);
                if (application == null)
                {
                    return false;
                }

                _logger.LogInformation("Deleting application: {AppCode}", application.AppCode);

                // Check if has any package versions
                var versions = await _unitOfWork.PackageVersions.GetByApplicationIdAsync(id);
                if (versions.Any())
                {
                    throw new Exception("Cannot delete application with existing package versions. Delete all versions first.");
                }

                _unitOfWork.Applications.Delete(application);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Application deleted successfully: ID {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting application ID: {Id}", id);
                throw;
            }
        }

        public async Task<ApplicationDetailResponse?> GetApplicationByIdAsync(int id)
        {
            var application = await _unitOfWork.Applications.GetByIdAsync(id);
            return application != null ? await MapToDetailResponseAsync(application) : null;
        }

        public async Task<ApplicationDetailResponse?> GetApplicationByCodeAsync(string appCode)
        {
            var application = await _unitOfWork.Applications.GetByAppCodeAsync(appCode);
            return application != null ? await MapToDetailResponseAsync(application) : null;
        }

        public async Task<IEnumerable<ApplicationDetailResponse>> GetAllApplicationsAsync()
        {
            var applications = await _unitOfWork.Applications.GetActiveApplicationsForAdminAsync();
            var responses = new List<ApplicationDetailResponse>();

            foreach (var app in applications)
            {
                responses.Add(await MapToDetailResponseAsync(app));
            }

            return responses;
        }

        public async Task<IEnumerable<ApplicationDetailResponse>> GetApplicationsByCategoryAsync(int categoryId)
        {
            var applications = await _unitOfWork.Applications.GetApplicationsByCategoryAsync(categoryId.ToString());
            var responses = new List<ApplicationDetailResponse>();
            foreach (var app in applications)
            {
                responses.Add(await MapToDetailResponseAsync(app));
            }

            return responses;
        }

        public async Task<ApplicationDetailResponse> GetApplicationWithStatsAsync(int id)
        {
            var application = await _unitOfWork.Applications.GetByIdAsync(id);
            if (application == null)
            {
                throw new Exception($"Application with ID {id} not found");
            }

            return await MapToDetailResponseAsync(application);
        }

        private async Task<ApplicationDetailResponse> MapToDetailResponseAsync(Application app)
        {
            // Get latest version
            var latestVersion = await _unitOfWork.PackageVersions.GetLatestVersionAsync(app.Id);

            var lastestManifest = await _unitOfWork.ApplicationManifests.GetLatestActiveManifestAsync(app.Id);

            // Get total versions count
            var versions = await _unitOfWork.PackageVersions.GetByApplicationIdAsync(app.Id);
            var totalVersions = versions.Count();

            // Get total storage size
            var totalStorage = await _unitOfWork.PackageVersions.GetTotalStorageSizeAsync(app.Id);

            // Get total installs
            var installLogs = await _unitOfWork.InstallationLogs.GetByApplicationIdAsync(app.Id);
            var totalInstalls = installLogs.Count();

            return new ApplicationDetailResponse
            {
                Id = app.Id,
                ManifestId = lastestManifest?.Id ?? 0,
                AppCode = app.AppCode,
                Name = app.Name,
                Description = app.Description,
                IconUrl = app.IconUrl,
                CategoryId = app.CategoryId,
                CategoryName = app.Category?.DisplayName,
                IsActive = app.IsActive,
                CreatedAt = app.CreatedAt,
                UpdatedAt = app.UpdatedAt,
                LatestVersion = latestVersion?.Version,
                LatestVersionDate = latestVersion?.PublishedAt ?? latestVersion?.UploadedAt,
                TotalVersions = totalVersions,
                TotalInstalls = totalInstalls,
                TotalStorageSize = totalStorage
            };
        }
    }
}