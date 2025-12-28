using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.UnitOfWork;
using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ClientLancher.Implement.Services
{
    public class PackageVersionService : IPackageVersionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PackageVersionService> _logger;
        private readonly string _packagesBasePath;

        public PackageVersionService(
            IUnitOfWork unitOfWork,
            ILogger<PackageVersionService> logger,
            Microsoft.AspNetCore.Hosting.IHostingEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _packagesBasePath = Path.Combine(environment.ContentRootPath, "Packages");
            Directory.CreateDirectory(_packagesBasePath);
        }

        public async Task<PackageVersionResponse> UploadPackageAsync(PackageUploadRequest request)
        {
            try
            {
                _logger.LogInformation("Starting package upload for Application ID: {ApplicationId}, Version: {Version}",
                    request.ApplicationId, request.Version);

                // 1. Validate application exists
                var application = await _unitOfWork.Applications.GetByIdAsync(request.ApplicationId);
                if (application == null)
                {
                    throw new Exception($"Application with ID {request.ApplicationId} not found");
                }

                // 2. Check if version already exists
                var existingVersion = await _unitOfWork.PackageVersions
                    .GetByApplicationAndVersionAsync(request.ApplicationId, request.Version);

                if (existingVersion != null)
                {
                    throw new Exception($"Version {request.Version} already exists for application {application.AppCode}");
                }

                // 3. Validate package file
                if (!await ValidatePackageAsync(request.PackageFile))
                {
                    throw new Exception("Invalid package file");
                }

                // 4. Calculate file hash
                using var stream = request.PackageFile.OpenReadStream();
                var fileHash = await CalculateFileHashAsync(stream);
                stream.Position = 0;

                // 5. Determine storage path
                //var fileName = $"{application.AppCode}_v{request.Version}.zip";
                var fileName = $"{application.AppCode}_{request.Version}.zip";
                var storagePath = Path.Combine(application.AppCode, request.Version, fileName);
                var fullPath = Path.Combine(_packagesBasePath, storagePath);

                // 6. Save file to disk
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await request.PackageFile.CopyToAsync(fileStream);
                }

                _logger.LogInformation("Package file saved to: {Path}", fullPath);

                // 7. Create database record
                var packageVersion = new PackageVersion
                {
                    ApplicationId = request.ApplicationId,
                    Version = request.Version,
                    PackageFileName = fileName,
                    PackageType = request.PackageType,
                    FileSizeBytes = request.PackageFile.Length,
                    FileHash = fileHash,
                    StoragePath = storagePath,
                    ReleaseNotes = request.ReleaseNotes,
                    IsActive = true,
                    IsStable = request.IsStable,
                    MinimumClientVersion = request.MinimumClientVersion,
                    UploadedBy = request.UploadedBy,
                    UploadedAt = DateTime.UtcNow,
                    PublishedAt = request.PublishImmediately ? DateTime.UtcNow : null
                };

                await _unitOfWork.PackageVersions.AddAsync(packageVersion);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Package version created successfully: ID {Id}", packageVersion.Id);

                // 8. Update manifest if published
                if (request.PublishImmediately)
                {
                    await UpdateManifestFileAsync(application.AppCode, packageVersion);
                }

                return MapToResponse(packageVersion, application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading package");
                throw;
            }
        }

        public async Task<PackageVersionResponse> UpdatePackageAsync(int id, PackageUpdateRequest request)
        {
            try
            {
                var package = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(id);
                if (package == null)
                {
                    throw new Exception($"Package version with ID {id} not found");
                }

                if (request.ReleaseNotes != null)
                    package.ReleaseNotes = request.ReleaseNotes;

                if (request.IsActive.HasValue)
                    package.IsActive = request.IsActive.Value;

                if (request.IsStable.HasValue)
                    package.IsStable = request.IsStable.Value;

                if (request.MinimumClientVersion != null)
                    package.MinimumClientVersion = request.MinimumClientVersion;

                _unitOfWork.PackageVersions.Update(package);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Package version updated: ID {Id}", id);

                return MapToResponse(package, package.Application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating package ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeletePackageAsync(int id)
        {
            try
            {
                var package = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(id);
                if (package == null)
                {
                    return false;
                }

                // Delete physical file
                var fullPath = Path.Combine(_packagesBasePath, package.StoragePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted package file: {Path}", fullPath);
                }

                // Delete from database
                _unitOfWork.PackageVersions.Delete(package);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Package version deleted: ID {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting package ID: {Id}", id);
                throw;
            }
        }

        public async Task<PackageVersionResponse> PublishPackageAsync(PublishPackageRequest request)
        {
            try
            {
                var package = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(request.PackageVersionId);
                if (package == null)
                {
                    throw new Exception($"Package version with ID {request.PackageVersionId} not found");
                }

                if (package.PublishedAt != null)
                {
                    throw new Exception("Package is already published");
                }

                package.PublishedAt = DateTime.UtcNow;
                _unitOfWork.PackageVersions.Update(package);
                await _unitOfWork.SaveChangesAsync();

                // Update manifest file
                await UpdateManifestFileAsync(package.Application.AppCode, package);

                _logger.LogInformation("Package published: ID {Id} by {User}",
                    request.PackageVersionId, request.PublishedBy);

                return MapToResponse(package, package.Application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing package ID: {Id}", request.PackageVersionId);
                throw;
            }
        }

        public async Task<PackageVersionResponse?> GetPackageByIdAsync(int id)
        {
            var package = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(id);
            return package != null ? MapToResponse(package, package.Application) : null;
        }

        public async Task<IEnumerable<PackageVersionResponse>> GetPackagesByApplicationIdAsync(int applicationId)
        {
            var packages = await _unitOfWork.PackageVersions.GetByApplicationIdAsync(applicationId);
            var application = await _unitOfWork.Applications.GetByIdAsync(applicationId);

            return packages.Select(p => MapToResponse(p, application!));
        }

        public async Task<PackageVersionResponse?> GetLatestVersionAsync(int applicationId, bool stableOnly = true)
        {
            var package = await _unitOfWork.PackageVersions.GetLatestVersionAsync(applicationId, stableOnly);
            if (package == null)
                return null;

            var application = await _unitOfWork.Applications.GetByIdAsync(applicationId);
            return MapToResponse(package, application!);
        }

        public async Task<IEnumerable<PackageVersionResponse>> GetVersionHistoryAsync(int applicationId, int take = 10)
        {
            var packages = await _unitOfWork.PackageVersions.GetVersionHistoryAsync(applicationId, take);
            var application = await _unitOfWork.Applications.GetByIdAsync(applicationId);

            return packages.Select(p => MapToResponse(p, application!));
        }

        public async Task<(byte[] fileData, string fileName, string contentType)> DownloadPackageAsync(int id)
        {
            try
            {
                var package = await _unitOfWork.PackageVersions.GetByIdAsync(id);
                if (package == null)
                {
                    throw new Exception($"Package version with ID {id} not found");
                }

                var fullPath = Path.Combine(_packagesBasePath, package.StoragePath);
                if (!File.Exists(fullPath))
                {
                    throw new Exception($"Package file not found: {fullPath}");
                }

                var fileData = await File.ReadAllBytesAsync(fullPath);

                // Update download count
                package.DownloadCount++;
                package.LastDownloadedAt = DateTime.UtcNow;
                _unitOfWork.PackageVersions.Update(package);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Package downloaded: ID {Id}, File: {FileName}", id, package.PackageFileName);

                return (fileData, package.PackageFileName, "application/zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading package ID: {Id}", id);
                throw;
            }
        }

        public async Task RecordDownloadStatisticAsync(int packageVersionId, string machineName, string userName,
            string ipAddress, bool success, long bytesDownloaded, int durationSeconds, string? error = null)
        {
            try
            {
                var statistic = new DownloadStatistic
                {
                    PackageVersionId = packageVersionId,
                    MachineName = machineName,
                    MachineId = $"{machineName}_{userName}",
                    UserName = userName,
                    IpAddress = ipAddress,
                    DownloadedAt = DateTime.UtcNow,
                    BytesDownloaded = bytesDownloaded,
                    DurationSeconds = durationSeconds,
                    Success = success,
                    ErrorMessage = error,
                    ClientLauncherVersion = "1.0.0", // TODO: Get from request header
                    OsVersion = Environment.OSVersion.ToString()
                };

                await _unitOfWork.DownloadStatistics.AddAsync(statistic);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Download statistic recorded for package {PackageVersionId}", packageVersionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording download statistic");
                // Don't throw - this is non-critical
            }
        }

        public async Task<PackageVersionResponse> RollbackToVersionAsync(int applicationId, string version, string performedBy)
        {
            try
            {
                _logger.LogInformation("Rolling back Application {ApplicationId} to version {Version}",
                    applicationId, version);

                var targetVersion = await _unitOfWork.PackageVersions
                    .GetByApplicationAndVersionAsync(applicationId, version);

                if (targetVersion == null)
                {
                    throw new Exception($"Version {version} not found for application");
                }

                var currentLatest = await _unitOfWork.PackageVersions.GetLatestVersionAsync(applicationId);

                // Create new version entry that replaces current
                var rollbackVersion = new PackageVersion
                {
                    ApplicationId = applicationId,
                    Version = $"{version}-rollback",
                    PackageFileName = targetVersion.PackageFileName,
                    PackageType = targetVersion.PackageType,
                    FileSizeBytes = targetVersion.FileSizeBytes,
                    FileHash = targetVersion.FileHash,
                    StoragePath = targetVersion.StoragePath,
                    ReleaseNotes = $"Rollback to version {version}. Performed by {performedBy}",
                    IsActive = true,
                    IsStable = true,
                    UploadedBy = performedBy,
                    UploadedAt = DateTime.UtcNow,
                    PublishedAt = DateTime.UtcNow,
                    ReplacesVersionId = currentLatest?.Id
                };

                await _unitOfWork.PackageVersions.AddAsync(rollbackVersion);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Rollback completed: New version ID {Id}", rollbackVersion.Id);

                var application = await _unitOfWork.Applications.GetByIdAsync(applicationId);
                return MapToResponse(rollbackVersion, application!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during rollback");
                throw;
            }
        }

        public async Task<Dictionary<string, IEnumerable<PackageVersionResponse>>> GetAllPackagesGroupedByApplicationAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all packages grouped by application");

                // Get all active applications
                var applications = await _unitOfWork.Applications.GetActiveApplicationsForAdminAsync();

                var result = new Dictionary<string, IEnumerable<PackageVersionResponse>>();

                foreach (var application in applications)
                {
                    // Get all packages for each application
                    var packages = await _unitOfWork.PackageVersions.GetByApplicationIdAsync(application.Id);

                    // Map to response objects
                    var packageResponses = packages.Select(p => MapToResponse(p, application));

                    // Add to dictionary with application name or AppCode as key
                    result[application.AppCode] = packageResponses;
                }

                _logger.LogInformation("Successfully grouped packages for {Count} applications", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all packages grouped by application");
                throw;
            }
        }

        public async Task<bool> ValidatePackageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Package file is null or empty");
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".zip")
            {
                _logger.LogWarning("Invalid file extension: {Extension}", extension);
                return false;
            }

            // Check file size (max 500MB)
            if (file.Length > 500 * 1024 * 1024)
            {
                _logger.LogWarning("File too large: {Size} bytes", file.Length);
                return false;
            }

            return await Task.FromResult(true);
        }

        public async Task<string> CalculateFileHashAsync(Stream fileStream)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(fileStream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private async Task UpdateManifestFileAsync(string appCode, PackageVersion package)
        {
            try
            {
                var manifestsBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Manifests");
                var manifestPath = Path.Combine(manifestsBasePath, appCode, "manifest.json");

                Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);

                var manifest = new AppManifest
                {
                    appCode = appCode,
                    binary = new BinaryInfo
                    {
                        version = package.Version,
                        package = package.PackageFileName
                    },
                    config = new ConfigInfo
                    {
                        version = package.Version,
                        package = package.PackageFileName,
                        mergeStrategy = "preserveLocal"
                    },
                    updatePolicy = new UpdatePolicy
                    {
                        type = "both",
                        force = false
                    }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(manifestPath, json);
                _logger.LogInformation("Manifest file updated for {AppCode}", appCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manifest file for {AppCode}", appCode);
            }
        }

        private PackageVersionResponse MapToResponse(PackageVersion package, Application application)
        {
            return new PackageVersionResponse
            {
                Id = package.Id,
                ApplicationId = package.ApplicationId,
                ApplicationName = application.Name,
                AppCode = application.AppCode,
                Version = package.Version,
                PackageFileName = package.PackageFileName,
                PackageType = package.PackageType,
                FileSizeBytes = package.FileSizeBytes,
                FileHash = package.FileHash,
                StoragePath = package.StoragePath,
                ReleaseNotes = package.ReleaseNotes,
                IsActive = package.IsActive,
                IsStable = package.IsStable,
                MinimumClientVersion = package.MinimumClientVersion,
                UploadedBy = package.UploadedBy,
                UploadedAt = package.UploadedAt,
                PublishedAt = package.PublishedAt,
                DownloadCount = package.DownloadCount,
                LastDownloadedAt = package.LastDownloadedAt,
                ReplacesVersionId = package.ReplacesVersionId,
                ReplacesVersionNumber = package.ReplacesVersion?.Version
            };
        }
    }
}