using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;

namespace ClientLancher.Implement.Services.Interface
{
    public interface IInstallationLogService
    {
        Task<InstallationLogPaginationResponse> GetInstallationLogByFilterAsync(InstallationLogFilterRequest request);
        Task<List<InstallationReportResponse>> GetInstallationReportByVersionAsync(InstallationReportRequest request);
        Task<List<InstallationLog>> GetSuccessfulByApplicationIdAsync(int applicationId);
    }
}