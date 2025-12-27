using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.UnitOfWork;
using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;

namespace ClientLancher.Implement.Services
{
    public class ManifestManagementService : IManifestManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ManifestManagementService> _logger;

        public ManifestManagementService(
            IUnitOfWork unitOfWork,
            ILogger<ManifestManagementService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ManifestResponse> CreateManifestAsync(int applicationId, ManifestCreateRequest request, string? createdBy = null)
        {
            try
            {
                // Validate application exists
                var application = await _unitOfWork.Applications.GetByIdAsync(applicationId);
                if (application == null)
                {
                    throw new Exception($"Application with ID {applicationId} not found");
                }

                // Check if version already exists
                var exists = await _unitOfWork.ApplicationManifests.VersionExistsAsync(applicationId, request.Version);
                if (exists)
                {
                    throw new Exception($"Manifest version {request.Version} already exists for this application");
                }

                var manifest = new ApplicationManifest
                {
                    ApplicationId = applicationId,
                    Version = request.Version,
                    BinaryVersion = request.BinaryVersion,
                    BinaryPackage = request.BinaryPackage,
                    ConfigVersion = request.ConfigVersion,
                    ConfigPackage = request.ConfigPackage,
                    ConfigMergeStrategy = request.ConfigMergeStrategy,
                    UpdateType = request.UpdateType,
                    ForceUpdate = request.ForceUpdate,
                    ReleaseNotes = request.ReleaseNotes,
                    IsStable = request.IsStable,
                    PublishedAt = request.PublishedAt ?? DateTime.UtcNow,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.ApplicationManifests.AddAsync(manifest);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created manifest version {Version} for application {AppCode}",
                    request.Version, application.AppCode);

                return MapToResponse(manifest, application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manifest for application ID {ApplicationId}", applicationId);
                throw;
            }
        }

        public async Task<ManifestResponse> UpdateManifestAsync(int manifestId, ManifestUpdateRequest request, string? updatedBy = null)
        {
            try
            {
                var manifest = await _unitOfWork.ApplicationManifests.GetByIdAsync(manifestId);
                if (manifest == null)
                {
                    throw new Exception($"Manifest with ID {manifestId} not found");
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(request.Version))
                    manifest.Version = request.Version;

                if (!string.IsNullOrEmpty(request.BinaryVersion))
                    manifest.BinaryVersion = request.BinaryVersion;

                if (!string.IsNullOrEmpty(request.BinaryPackage))
                    manifest.BinaryPackage = request.BinaryPackage;

                if (request.ConfigVersion != null)
                    manifest.ConfigVersion = request.ConfigVersion;

                if (request.ConfigPackage != null)
                    manifest.ConfigPackage = request.ConfigPackage;

                if (request.ConfigMergeStrategy != null)
                    manifest.ConfigMergeStrategy = request.ConfigMergeStrategy;

                if (request.UpdateType != null)
                    manifest.UpdateType = request.UpdateType;

                if (request.ForceUpdate.HasValue)
                    manifest.ForceUpdate = request.ForceUpdate.Value;

                if (request.ReleaseNotes != null)
                    manifest.ReleaseNotes = request.ReleaseNotes;

                if (request.IsActive.HasValue)
                    manifest.IsActive = request.IsActive.Value;

                if (request.IsStable.HasValue)
                    manifest.IsStable = request.IsStable.Value;

                manifest.UpdatedAt = DateTime.UtcNow;
                manifest.UpdatedBy = updatedBy;

                _unitOfWork.ApplicationManifests.Update(manifest);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Updated manifest ID {ManifestId}", manifestId);

                return MapToResponse(manifest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manifest ID {ManifestId}", manifestId);
                throw;
            }
        }

        public async Task<bool> DeleteManifestAsync(int manifestId)
        {
            try
            {
                var manifest = await _unitOfWork.ApplicationManifests.GetByIdAsync(manifestId);
                if (manifest == null)
                {
                    return false;
                }

                _unitOfWork.ApplicationManifests.Remove(manifest);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deleted manifest ID {ManifestId}", manifestId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting manifest ID {ManifestId}", manifestId);
                throw;
            }
        }

        public async Task<ManifestResponse?> GetManifestByIdAsync(int manifestId)
        {
            var manifest = await _unitOfWork.ApplicationManifests.GetByIdAsync(manifestId);
            return manifest == null ? null : MapToResponse(manifest);
        }

        public async Task<ManifestResponse?> GetLatestManifestAsync(int applicationId)
        {
            var manifest = await _unitOfWork.ApplicationManifests.GetLatestActiveManifestAsync(applicationId);
            return manifest == null ? null : MapToResponse(manifest);
        }

        public async Task<ManifestResponse?> GetLatestManifestByAppCodeAsync(string appCode)
        {
            var manifest = await _unitOfWork.ApplicationManifests.GetLatestActiveManifestByAppCodeAsync(appCode);
            return manifest == null ? null : MapToResponse(manifest);
        }

        public async Task<ManifestResponse?> GetManifestByVersionAsync(int applicationId, string version)
        {
            var manifest = await _unitOfWork.ApplicationManifests.GetByVersionAsync(applicationId, version);
            return manifest == null ? null : MapToResponse(manifest);
        }

        public async Task<IEnumerable<ManifestResponse>> GetManifestHistoryAsync(int applicationId, int take = 10)
        {
            var manifests = await _unitOfWork.ApplicationManifests.GetManifestHistoryAsync(applicationId, take);

            return manifests.Select(manifest => new ManifestResponse
            {
                Id = manifest.Id,
                ApplicationId = manifest.ApplicationId,
                AppCode = manifest.Application?.AppCode ?? string.Empty,
                AppName = manifest.Application?.Name ?? string.Empty,
                Version = manifest.Version,
                BinaryVersion = manifest.BinaryVersion,
                BinaryPackage = manifest.BinaryPackage,
                ConfigVersion = manifest.ConfigVersion,
                ConfigPackage = manifest.ConfigPackage,
                ConfigMergeStrategy = manifest.ConfigMergeStrategy,
                UpdateType = manifest.UpdateType,
                ForceUpdate = manifest.ForceUpdate,
                ReleaseNotes = manifest.ReleaseNotes,
                IsActive = manifest.IsActive,
                IsStable = manifest.IsStable,
                PublishedAt = manifest.PublishedAt,
                CreatedAt = manifest.CreatedAt,
                CreatedBy = manifest.CreatedBy
            });
            //return manifests.Select(MapToResponse);
        }

        public async Task<ManifestJsonResponse?> GenerateManifestJsonAsync(string appCode)
        {
            try
            {
                var manifest = await _unitOfWork.ApplicationManifests.GetLatestActiveManifestByAppCodeAsync(appCode);
                if (manifest == null)
                {
                    _logger.LogWarning("No active manifest found for {AppCode}", appCode);
                    return null;
                }

                var response = new ManifestJsonResponse
                {
                    AppCode = manifest.Application.AppCode,
                    Binary = new ManifestJsonResponse.BinaryInfo
                    {
                        Version = manifest.BinaryVersion,
                        Package = manifest.BinaryPackage
                    },
                    Config = new ManifestJsonResponse.ConfigInfo
                    {
                        Version = manifest.ConfigVersion ?? manifest.BinaryVersion,
                        Package = manifest.ConfigPackage ?? manifest.BinaryPackage,
                        MergeStrategy = manifest.ConfigMergeStrategy
                    },
                    UpdatePolicy = new ManifestJsonResponse.UpdatePolicyInfo
                    {
                        Type = manifest.UpdateType,
                        Force = manifest.ForceUpdate
                    }
                };

                _logger.LogInformation("Generated manifest JSON for {AppCode} version {Version}",
                    appCode, manifest.Version);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating manifest JSON for {AppCode}", appCode);
                throw;
            }
        }

        public async Task<bool> ActivateManifestAsync(int manifestId)
        {
            try
            {
                var manifest = await _unitOfWork.ApplicationManifests.GetByIdAsync(manifestId);
                if (manifest == null)
                {
                    return false;
                }

                // Deactivate all other manifests for this application
                await _unitOfWork.ApplicationManifests.DeactivateAllManifestsAsync(manifest.ApplicationId);

                // Activate this manifest
                manifest.IsActive = true;
                manifest.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ApplicationManifests.Update(manifest);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Activated manifest ID {ManifestId} version {Version}",
                    manifestId, manifest.Version);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating manifest ID {ManifestId}", manifestId);
                throw;
            }
        }

        public async Task<bool> DeactivateManifestAsync(int manifestId)
        {
            try
            {
                var manifest = await _unitOfWork.ApplicationManifests.GetByIdAsync(manifestId);
                if (manifest == null)
                {
                    return false;
                }

                manifest.IsActive = false;
                manifest.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ApplicationManifests.Update(manifest);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deactivated manifest ID {ManifestId}", manifestId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating manifest ID {ManifestId}", manifestId);
                throw;
            }
        }

        private ManifestResponse MapToResponse(ApplicationManifest manifest, Application? app = null)
        {
            var application = app ?? manifest.Application;

            return new ManifestResponse
            {
                Id = manifest.Id,
                ApplicationId = manifest.ApplicationId,
                AppCode = application?.AppCode ?? string.Empty,
                AppName = application?.Name ?? string.Empty,
                Version = manifest.Version,
                BinaryVersion = manifest.BinaryVersion,
                BinaryPackage = manifest.BinaryPackage,
                ConfigVersion = manifest.ConfigVersion,
                ConfigPackage = manifest.ConfigPackage,
                ConfigMergeStrategy = manifest.ConfigMergeStrategy,
                UpdateType = manifest.UpdateType,
                ForceUpdate = manifest.ForceUpdate,
                ReleaseNotes = manifest.ReleaseNotes,
                IsActive = manifest.IsActive,
                IsStable = manifest.IsStable,
                PublishedAt = manifest.PublishedAt,
                CreatedAt = manifest.CreatedAt,
                CreatedBy = manifest.CreatedBy
            };
        }
    }
}