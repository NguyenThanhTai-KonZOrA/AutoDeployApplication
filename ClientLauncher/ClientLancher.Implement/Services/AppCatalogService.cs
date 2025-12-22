using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace ClientLancher.Implement.Services
{
    public class AppCatalogService : IAppCatalogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVersionService _versionService;
        private readonly ILogger<AppCatalogService> _logger;

        public AppCatalogService(
            IUnitOfWork unitOfWork,
            IVersionService versionService,
            ILogger<AppCatalogService> logger)
        {
            _unitOfWork = unitOfWork;
            _versionService = versionService;
            _logger = logger;
        }

        public async Task<IEnumerable<Application>> GetAllApplicationsAsync()
        {
            _logger.LogInformation("Fetching all active applications");
            return await _unitOfWork.Applications.GetActiveApplicationsAsync();
        }

        public async Task<IEnumerable<Application>> GetApplicationsByCategoryAsync(string category)
        {
            _logger.LogInformation("Fetching applications for category: {Category}", category);
            return await _unitOfWork.Applications.GetApplicationsByCategoryAsync(category);
        }

        public async Task<Application?> GetApplicationAsync(string appCode)
        {
            _logger.LogInformation("Fetching application: {AppCode}", appCode);
            return await _unitOfWork.Applications.GetByAppCodeAsync(appCode);
        }

        public async Task<bool> IsApplicationInstalledAsync(string appCode)
        {
            var localInfo = _versionService.GetLocalVersions(appCode);
            var isInstalled = localInfo.BinaryVersion != "0.0.0";

            _logger.LogInformation("Application {AppCode} installed: {IsInstalled}", appCode, isInstalled);
            return await Task.FromResult(isInstalled);
        }

        public async Task<string?> GetInstalledVersionAsync(string appCode)
        {
            var localInfo = _versionService.GetLocalVersions(appCode);
            var version = localInfo.BinaryVersion != "0.0.0" ? localInfo.BinaryVersion : null;

            _logger.LogInformation("Application {AppCode} installed version: {Version}", appCode, version ?? "Not installed");
            return await Task.FromResult(version);
        }
    }
}