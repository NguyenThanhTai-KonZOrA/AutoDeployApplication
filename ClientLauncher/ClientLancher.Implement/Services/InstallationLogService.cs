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

        public async Task<List<InstallationReportResponse>> GetInstallationReportByVersionAsync(InstallationReportRequest request)
        {
            var logs = await _installationLogRepository.GetInstallationReportDataAsync(request);

            // Group by Application and Version
            var groupedByApp = logs
                .GroupBy(l => new { l.ApplicationId, l.Application.Name, l.Application.AppCode })
                .Select(appGroup => new InstallationReportResponse
                {
                    ApplicationId = appGroup.Key.ApplicationId,
                    ApplicationName = appGroup.Key.Name,
                    AppCode = appGroup.Key.AppCode,
                    VersionStats = appGroup
                        .GroupBy(l => l.NewVersion)
                        .Select(versionGroup =>
                        {
                            // Get latest log for each machine (by MachineId or MachineName)
                            var latestByMachine = versionGroup
                                .GroupBy(l => string.IsNullOrEmpty(l.MachineId) ? l.MachineName : l.MachineId)
                                .Select(machineGroup => machineGroup.OrderByDescending(l => l.CreatedAt).First())
                                .ToList();

                            return new VersionInstallationStats
                            {
                                Version = versionGroup.Key,
                                PCCount = latestByMachine.Count,
                                PCs = latestByMachine.Select(l => new PCInstallationDetail
                                {
                                    MachineName = l.MachineName,
                                    MachineId = l.MachineId,
                                    UserName = l.UserName,
                                    Status = l.Status,
                                    Action = l.Action,
                                    InstalledAt = l.StartedAt,
                                    LastUpdatedAt = l.CompletedAt
                                })
                                .OrderBy(p => p.MachineName)
                                .ToList()
                            };
                        })
                        .OrderByDescending(v => v.Version)
                        .ToList(),
                    TotalPCs = appGroup
                        .GroupBy(l => string.IsNullOrEmpty(l.MachineId) ? l.MachineName : l.MachineId)
                        .Count()
                })
                .OrderBy(a => a.ApplicationName)
                .ToList();

            return groupedByApp;
        }
    }
}
