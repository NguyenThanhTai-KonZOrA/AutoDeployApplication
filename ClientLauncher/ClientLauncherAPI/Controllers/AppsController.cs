using ClientLancher.Implement.Services;
using ClientLancher.Implement.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [ApiController]
    [Route("api/apps")]
    public class AppsController : ControllerBase
    {
        private readonly IAppCatalogService _appCatalogService;
        private readonly IManifestService _manifestService;
        private readonly ILogger<AppsController> _logger;
        private readonly string _packagesBasePath;
        private readonly string _manifestsBasePath;
        private readonly IWebHostEnvironment _environment;
        private readonly IPackageVersionService _packageVersionService;

        public AppsController(
            IAppCatalogService appCatalogService,
            IManifestService manifestService,
            ILogger<AppsController> logger,
            IWebHostEnvironment environment,
            IPackageVersionService packageVersionService)
        {
            _appCatalogService = appCatalogService;
            _manifestService = manifestService;
            _logger = logger;
            _environment = environment;
            _packageVersionService = packageVersionService;
            _packagesBasePath = Path.Combine(_environment.ContentRootPath, "Packages");
            _manifestsBasePath = Path.Combine(_environment.ContentRootPath, "Manifests");

            // Ensure directories exist
            Directory.CreateDirectory(_packagesBasePath);
            Directory.CreateDirectory(_manifestsBasePath);

            _logger.LogInformation("Packages path: {PackagesPath}", _packagesBasePath);
            _logger.LogInformation("Manifests path: {ManifestsPath}", _manifestsBasePath);
        }

        /// <summary>
        /// Get all available applications
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllApplications()
        {
            try
            {
                var apps = await _appCatalogService.GetAllApplicationsAsync();
                return Ok(apps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications");
                return StatusCode(500, "");
            }
        }

        /// <summary>
        /// Download package file (binary or config)
        /// </summary>
        [HttpGet("{appCode}/download/{packageName}")]
        public async Task<IActionResult> DownloadPackage(string appCode, string packageName)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var machineName = Request.Headers["X-Machine-Name"].ToString() ?? "Unknown";
            var userName = Request.Headers["X-User-Name"].ToString() ?? "Unknown";
            try
            {
                _logger.LogInformation("Download request for {AppCode}/{PackageName}", appCode, packageName);

                // Security: Validate package name to prevent directory traversal
                if (packageName.Contains("..") || packageName.Contains("/") || packageName.Contains("\\"))
                {
                    _logger.LogWarning("Invalid package name detected: {PackageName}", packageName);
                    return BadRequest("");
                }

                // Check if app exists
                var app = await _appCatalogService.GetApplicationAsync(appCode);
                if (app == null || app.PackageVersions == null)
                {
                    _logger.LogWarning("Application {AppCode} not found", appCode);
                    return NotFound();
                }

                // Construct file path
                string filePath = string.Empty;
                if (app.PackageVersions.Any())
                {
                    var fileAppPath = app?.PackageVersions?.FirstOrDefault(x => x.PackageFileName == packageName);

                    filePath = Path.Combine(_packagesBasePath, fileAppPath?.StoragePath);
                }
                else
                {
                    filePath = Path.Combine(_packagesBasePath, appCode, packageName);
                }

                _logger.LogInformation("Looking for package at: {FilePath}", filePath);

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("Package file not found: {FilePath}", filePath);

                    // List available files for debugging
                    var appPackageDir = Path.Combine(_packagesBasePath, appCode);
                    if (Directory.Exists(appPackageDir))
                    {
                        var files = Directory.GetFiles(appPackageDir);
                        _logger.LogInformation("Available files in {AppCode}: {Files}", appCode, string.Join(", ", files.Select(Path.GetFileName)));
                    }
                    else
                    {
                        _logger.LogWarning("Package directory does not exist: {Dir}", appPackageDir);
                        _logger.LogWarning("Base packages path: {BasePath}", _packagesBasePath);

                        // List all directories in Packages folder
                        if (Directory.Exists(_packagesBasePath))
                        {
                            var dirs = Directory.GetDirectories(_packagesBasePath);
                            _logger.LogInformation("Available app directories: {Dirs}", string.Join(", ", dirs.Select(Path.GetFileName)));
                        }
                    }
                    // Record download failure statistic
                    await _packageVersionService.UpdatePackageDownloadCountAsync(app?.PackageVersions?.LastOrDefault(x => x.PackageFileName == packageName)?.Id ?? 0);
                    await _packageVersionService.RecordDownloadStatisticAsync(
                        app?.PackageVersions.LastOrDefault()?.Id ?? 0, machineName, userName, ipAddress, false, 0, 0);

                    return NotFound();
                }

                // Read file
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                _logger.LogInformation("Successfully read {Size} bytes from {PackageName}", fileBytes.Length, packageName);

                // Determine content type based on extension
                var contentType = packageName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                    ? "application/zip"
                    : packageName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                        ? "application/json"
                        : "application/octet-stream";

                // Record download success statistic
                await _packageVersionService.UpdatePackageDownloadCountAsync(app?.PackageVersions?.LastOrDefault(x => x.PackageFileName == packageName)?.Id ?? 0);
                await _packageVersionService.RecordDownloadStatisticAsync(
                    app?.PackageVersions.LastOrDefault()?.Id ?? 0, machineName, userName, ipAddress, true, fileBytes.Length, 0);

                return File(fileBytes, contentType, packageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading package {AppCode}/{PackageName}", appCode, packageName);
                // Record download failure statistic
                await _packageVersionService.RecordDownloadStatisticAsync(
                     0, machineName, userName, ipAddress, false, 0, 0, $"Error downloading package {appCode}/{packageName}");

                return StatusCode(500, "");
            }
        }

        /// <summary>
        /// Health check endpoint for testing
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            var packagesExists = Directory.Exists(_packagesBasePath);
            var manifestsExists = Directory.Exists(_manifestsBasePath);

            // List all app directories
            var appDirectories = packagesExists
                ? Directory.GetDirectories(_packagesBasePath).Select(Path.GetFileName).ToList()
                : new List<string>();

            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                environment = new
                {
                    contentRootPath = _environment.ContentRootPath,
                    webRootPath = _environment.WebRootPath,
                    environmentName = _environment.EnvironmentName
                },
                paths = new
                {
                    packagesPath = _packagesBasePath,
                    manifestsPath = _manifestsBasePath,
                    packagesExists = packagesExists,
                    manifestsExists = manifestsExists,
                    availableApps = appDirectories
                }
            });
        }
    }
}