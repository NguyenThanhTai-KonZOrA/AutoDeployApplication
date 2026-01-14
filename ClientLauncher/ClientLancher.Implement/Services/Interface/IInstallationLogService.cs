using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IInstallationLogService
    {
        Task<InstallationLogPaginationResponse> GetInstallationLogByFilterAsync(InstallationLogFilterRequest request);
        Task<List<InstallationReportResponse>> GetInstallationReportByVersionAsync(InstallationReportRequest request);
        Task<List<InstallationLog>> GetSuccessfulByApplicationIdAsync(int applicationId);
    }
}