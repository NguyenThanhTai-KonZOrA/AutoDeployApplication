using Azure.Core;
using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;

namespace ClientLancher.Implement.Services
{
    public class InstallationLogService : IInstallationLogService
    {
        private readonly IInstallationLogRepository _installationLogRepository;
        public InstallationLogService(IInstallationLogRepository installationLogRepository)
        {
            _installationLogRepository = installationLogRepository;
        }

        public async Task<InstallationLogPaginationResponse> GetInstallationLogByFilterAsync(InstallationLogFilterRequest request)
        {
            var logs = await _installationLogRepository.GetPaginatedInstallationLogsAsync(request);

            // Get total count with same filters applied
            var totalRecords = await _installationLogRepository.GetFilteredCountAsync(request);
            return new InstallationLogPaginationResponse
            {
                Logs = logs.Select(MapToResponse).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalRecords = totalRecords
            };
        }

        private InstallationLogResponse MapToResponse(InstallationLog log)
        {
            return new InstallationLogResponse
            {
                Action = log.Action,
                ApplicationId = log.ApplicationId,
                ApplicationName = log.Application.Name,
                CompletedAt = log.CompletedAt,
                DurationInSeconds = log.DurationInSeconds,
                ErrorMessage = log.ErrorMessage,
                Id = log.Id,
                InstallationPath = log.InstallationPath,
                MachineId = log.MachineId,
                MachineName = log.MachineName,
                NewVersion = log.NewVersion,
                OldVersion = log.OldVersion,
                StartedAt = log.StartedAt,
                StackTrace = log.StackTrace,
                Status = log.Status,
                UserName = log.UserName
            };
        }
    }
}
