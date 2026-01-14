using Azure.Core;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.Services
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
                .Select(appGroup =>
                {
                    // Get latest version of this application
                    var latestVersion = appGroup
                        .Select(l => l.NewVersion)
                        .Distinct()
                        .OrderByDescending(v => v)
                        .FirstOrDefault();

                    // Get latest log for each machine (all versions)
                    var allLatestByMachine = appGroup
                        .GroupBy(l => string.IsNullOrEmpty(l.MachineId) ? l.MachineName : l.MachineId)
                        .Select(machineGroup => machineGroup.OrderByDescending(l => l.CreatedAt).First())
                        .ToList();

                    return new InstallationReportResponse
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

                                // Count devices that have been updated and not updated
                                var currentVersion = versionGroup.Key;

                                // Number of machines updated: machines with Action = "Update" or "Install" for this version
                                var updatedPCCount = latestByMachine
                                    .Count(l => l.Action.Equals("Update", StringComparison.OrdinalIgnoreCase) ||
                                               l.Action.Equals("Install", StringComparison.OrdinalIgnoreCase));

                                // Number of machines not updated: total machines in app - machines at this version
                                var notUpdatedPCCount = allLatestByMachine
                                    .Count(l => !l.NewVersion.Equals(currentVersion, StringComparison.OrdinalIgnoreCase));

                                return new VersionInstallationStats
                                {
                                    Version = versionGroup.Key,
                                    PCCount = latestByMachine.Count,
                                    UpdatedPCCount = updatedPCCount,
                                    NotUpdatedPCCount = notUpdatedPCCount,
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
                    };
                })
                .OrderBy(a => a.ApplicationName)
                .ToList();

            return groupedByApp;
        }

        public async Task<List<InstallationLog>> GetSuccessfulByApplicationIdAsync(int applicationId)
        {
            var logs = await _installationLogRepository.GetSuccessfulByApplicationIdAsync(applicationId);
            return logs.ToList();
        }
    }
}
