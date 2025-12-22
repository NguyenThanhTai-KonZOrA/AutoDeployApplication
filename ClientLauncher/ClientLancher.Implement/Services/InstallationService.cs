using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.UnitOfWork;
using ClientLancher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;

namespace ClientLancher.Implement.Services
{
    public class InstallationService : IInstallationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IManifestService _manifestService;
        private readonly IVersionService _versionService;
        private readonly IAppCatalogService _appCatalogService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<InstallationService> _logger;
        private readonly string _serverUrl;
        private readonly string _appsBasePath;

        public InstallationService(
            IUnitOfWork unitOfWork,
            IManifestService manifestService,
            IVersionService versionService,
            IAppCatalogService appCatalogService,
            HttpClient httpClient,
            ILogger<InstallationService> logger)
        {
            _unitOfWork = unitOfWork;
            _manifestService = manifestService;
            _versionService = versionService;
            _appCatalogService = appCatalogService;
            _httpClient = httpClient;
            _logger = logger;
            _serverUrl = "https://localhost:7172"; // Load from config
            _appsBasePath = "C:\\CompanyApps";
        }

        public async Task<InstallationResult> InstallApplicationAsync(string appCode, string userName)
        {
            var log = await CreateInstallationLogAsync(appCode, userName, "Install");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. Check if already installed
                var isInstalled = await _appCatalogService.IsApplicationInstalledAsync(appCode);
                if (isInstalled)
                {
                    return new InstallationResult
                    {
                        Success = true,
                        Message = "Application is already installed. Use Update instead."
                    };
                }

                // 2. Get manifest
                var manifest = await _manifestService.GetManifestAsync(appCode);
                if (manifest == null)
                {
                    throw new Exception("Manifest not found");
                }

                // 3. Download and extract
                var appPath = Path.Combine(_appsBasePath, appCode, "App");
                await DownloadAndExtractAsync(appCode, manifest.binary.package, appPath);

                // 4. Save version
                _versionService.SaveBinaryVersion(appCode, manifest.binary.version);

                // 5. Download config if exists
                if (!string.IsNullOrEmpty(manifest.config.package))
                {
                    var configPath = Path.Combine(_appsBasePath, appCode, "Config");
                    await DownloadAndExtractAsync(appCode, manifest.config.package, configPath);
                    _versionService.SaveConfigVersion(appCode, manifest.config.version);
                }

                stopwatch.Stop();
                await CompleteInstallationLogAsync(log, "Success", null, manifest.binary.version, stopwatch.Elapsed);

                return new InstallationResult
                {
                    Success = true,
                    Message = "Installation completed successfully",
                    InstalledVersion = manifest.binary.version
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Installation failed for {AppCode}", appCode);
                stopwatch.Stop();
                await CompleteInstallationLogAsync(log, "Failed", ex.Message, null, stopwatch.Elapsed, ex.StackTrace);

                return new InstallationResult
                {
                    Success = false,
                    Message = "Installation failed",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<InstallationResult> UpdateApplicationAsync(string appCode, string userName)
        {
            var log = await CreateInstallationLogAsync(appCode, userName, "Update");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var oldVersion = await _appCatalogService.GetInstalledVersionAsync(appCode);
                if (oldVersion == null)
                {
                    return new InstallationResult
                    {
                        Success = false,
                        Message = "Application is not installed. Use Install instead."
                    };
                }

                log.OldVersion = oldVersion;

                var manifest = await _manifestService.GetManifestAsync(appCode);
                if (manifest == null)
                {
                    throw new Exception("Manifest not found");
                }

                if (!_versionService.IsNewerVersion(manifest.binary.version, oldVersion))
                {
                    return new InstallationResult
                    {
                        Success = false,
                        Message = "No updates available"
                    };
                }

                // Backup current version
                var appPath = Path.Combine(_appsBasePath, appCode, "App");
                var backupPath = Path.Combine(_appsBasePath, appCode, $"Backup_{DateTime.Now:yyyyMMddHHmmss}");

                if (Directory.Exists(appPath))
                {
                    Directory.Move(appPath, backupPath);
                }

                try
                {
                    await DownloadAndExtractAsync(appCode, manifest.binary.package, appPath);
                    _versionService.SaveBinaryVersion(appCode, manifest.binary.version);

                    // Delete backup if successful
                    if (Directory.Exists(backupPath))
                    {
                        Directory.Delete(backupPath, true);
                    }

                    stopwatch.Stop();
                    await CompleteInstallationLogAsync(log, "Success", null, manifest.binary.version, stopwatch.Elapsed);

                    return new InstallationResult
                    {
                        Success = true,
                        Message = "Update completed successfully",
                        InstalledVersion = manifest.binary.version
                    };
                }
                catch
                {
                    // Rollback
                    if (Directory.Exists(appPath))
                    {
                        Directory.Delete(appPath, true);
                    }
                    if (Directory.Exists(backupPath))
                    {
                        Directory.Move(backupPath, appPath);
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update failed for {AppCode}", appCode);
                stopwatch.Stop();
                await CompleteInstallationLogAsync(log, "Failed", ex.Message, null, stopwatch.Elapsed, ex.StackTrace);

                return new InstallationResult
                {
                    Success = false,
                    Message = "Update failed",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<InstallationResult> UninstallApplicationAsync(string appCode, string userName)
        {
            var log = await CreateInstallationLogAsync(appCode, userName, "Uninstall");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var appFolder = Path.Combine(_appsBasePath, appCode);
                if (Directory.Exists(appFolder))
                {
                    Directory.Delete(appFolder, true);
                }

                stopwatch.Stop();
                await CompleteInstallationLogAsync(log, "Success", null, null, stopwatch.Elapsed);

                return new InstallationResult
                {
                    Success = true,
                    Message = "Uninstallation completed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uninstall failed for {AppCode}", appCode);
                stopwatch.Stop();
                await CompleteInstallationLogAsync(log, "Failed", ex.Message, null, stopwatch.Elapsed, ex.StackTrace);

                return new InstallationResult
                {
                    Success = false,
                    Message = "Uninstallation failed",
                    ErrorDetails = ex.Message
                };
            }
        }

        private async Task DownloadAndExtractAsync(string appCode, string packageName, string targetPath)
        {
            // ✅ FIX: Thêm /api/ vào URL và sử dụng endpoint download
            var packageUrl = $"{_serverUrl}/api/apps/{appCode}/download/{packageName}";
            var tempZip = Path.Combine(Path.GetTempPath(), $"{appCode}_{Guid.NewGuid()}.zip");

            try
            {
                _logger.LogInformation("Downloading package {PackageName} from {Url}", packageName, packageUrl);

                // ✅ Thêm error handling chi tiết hơn
                var response = await _httpClient.GetAsync(packageUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to download package. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException(
                        $"Failed to download package '{packageName}'. Status: {response.StatusCode}. " +
                        $"URL: {packageUrl}. Response: {errorContent}");
                }

                var packageData = await response.Content.ReadAsByteArrayAsync();

                if (packageData == null || packageData.Length == 0)
                {
                    throw new Exception($"Downloaded package '{packageName}' is empty");
                }

                _logger.LogInformation("Downloaded {Size} bytes", packageData.Length);

                await File.WriteAllBytesAsync(tempZip, packageData);

                _logger.LogInformation("Extracting to {Path}", targetPath);

                // ✅ Đảm bảo thư mục target tồn tại và trống
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
                Directory.CreateDirectory(targetPath);

                ZipFile.ExtractToDirectory(tempZip, targetPath, overwriteFiles: true);

                _logger.LogInformation("Successfully extracted package to {Path}", targetPath);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error downloading package {PackageName} from {Url}", packageName, packageUrl);
                throw new Exception($"Failed to download package '{packageName}' from server. Check if the file exists and the URL is correct: {packageUrl}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading or extracting package {PackageName}", packageName);
                throw;
            }
            finally
            {
                if (File.Exists(tempZip))
                {
                    try
                    {
                        File.Delete(tempZip);
                        _logger.LogDebug("Cleaned up temporary file: {TempZip}", tempZip);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary file: {TempZip}", tempZip);
                    }
                }
            }
        }

        private async Task<InstallationLog> CreateInstallationLogAsync(string appCode, string userName, string action)
        {
            var app = await _appCatalogService.GetApplicationAsync(appCode);
            if (app == null)
            {
                throw new Exception($"Application {appCode} not found");
            }

            var log = new InstallationLog
            {
                ApplicationId = app.Id,
                UserName = userName,
                MachineName = Environment.MachineName,
                MachineId = GetMachineId(),
                Action = action,
                Status = "InProgress",
                InstallationPath = Path.Combine(_appsBasePath, appCode),
                StartedAt = DateTime.UtcNow
            };

            await _unitOfWork.InstallationLogs.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();

            return log;
        }

        private async Task CompleteInstallationLogAsync(
            InstallationLog log,
            string status,
            string? errorMessage,
            string? newVersion,
            TimeSpan duration,
            string? stackTrace = null)
        {
            log.Status = status;
            log.ErrorMessage = errorMessage;
            log.StackTrace = stackTrace;
            log.NewVersion = newVersion ?? log.NewVersion;
            log.CompletedAt = DateTime.UtcNow;
            log.DurationInSeconds = (int)duration.TotalSeconds;

            _unitOfWork.InstallationLogs.Update(log);
            await _unitOfWork.SaveChangesAsync();
        }

        private string GetMachineId()
        {
            // Generate unique machine ID (có thể dùng CPU ID, MAC address, etc.)
            return Environment.MachineName + "_" + Environment.UserName;
        }
    }
}