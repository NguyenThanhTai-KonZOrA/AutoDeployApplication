using Azure.Core;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;
using ClosedXML.Excel;

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

        public async Task<byte[]> ExportInstallationReportToExcelAsync(InstallationReportRequest request)
        {
            var reportData = await GetInstallationReportByVersionAsync(request);

            using var workbook = new XLWorkbook();

            foreach (var appReport in reportData)
            {
                // Create a worksheet for each application
                var worksheet = workbook.Worksheets.Add(SanitizeSheetName(appReport.ApplicationName));

                // Set header row
                worksheet.Cell(1, 1).Value = "Application Information";
                worksheet.Range(1, 1, 1, 8).Merge();
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 14;
                worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
                worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;

                // App details
                worksheet.Cell(2, 1).Value = "Application Name:";
                worksheet.Cell(2, 2).Value = appReport.ApplicationName;
                worksheet.Cell(2, 1).Style.Font.Bold = true;

                worksheet.Cell(3, 1).Value = "App Code:";
                worksheet.Cell(3, 2).Value = appReport.AppCode;
                worksheet.Cell(3, 1).Style.Font.Bold = true;

                worksheet.Cell(4, 1).Value = "Total PCs:";
                worksheet.Cell(4, 2).Value = appReport.TotalPCs;
                worksheet.Cell(4, 1).Style.Font.Bold = true;

                int currentRow = 6;

                // Version statistics
                foreach (var versionStat in appReport.VersionStats)
                {
                    // Version header
                    worksheet.Cell(currentRow, 1).Value = $"Version: {versionStat.Version}";
                    worksheet.Range(currentRow, 1, currentRow, 8).Merge();
                    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 1).Style.Font.FontSize = 12;
                    worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(217, 217, 217);
                    currentRow++;

                    // Version stats
                    worksheet.Cell(currentRow, 1).Value = "PC Count:";
                    worksheet.Cell(currentRow, 2).Value = versionStat.PCCount;
                    worksheet.Cell(currentRow, 3).Value = "Updated:";
                    worksheet.Cell(currentRow, 4).Value = versionStat.UpdatedPCCount;
                    worksheet.Cell(currentRow, 5).Value = "Not Updated:";
                    worksheet.Cell(currentRow, 6).Value = versionStat.NotUpdatedPCCount;
                    currentRow++;

                    // PC details header
                    worksheet.Cell(currentRow, 1).Value = "Machine Name";
                    worksheet.Cell(currentRow, 2).Value = "Machine ID";
                    worksheet.Cell(currentRow, 3).Value = "User Name";
                    worksheet.Cell(currentRow, 4).Value = "Status";
                    worksheet.Cell(currentRow, 5).Value = "Action";
                    worksheet.Cell(currentRow, 6).Value = "Installed At";
                    worksheet.Cell(currentRow, 7).Value = "Last Updated At";

                    var headerRange = worksheet.Range(currentRow, 1, currentRow, 7);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(189, 215, 238);
                    headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    currentRow++;

                    // PC details
                    foreach (var pc in versionStat.PCs)
                    {
                        worksheet.Cell(currentRow, 1).Value = pc.MachineName;
                        worksheet.Cell(currentRow, 2).Value = pc.MachineId;
                        worksheet.Cell(currentRow, 3).Value = pc.UserName;
                        worksheet.Cell(currentRow, 4).Value = pc.Status;
                        worksheet.Cell(currentRow, 5).Value = pc.Action;
                        worksheet.Cell(currentRow, 6).Value = pc.InstalledAt.ToString("yyyy-MM-dd HH:mm:ss");
                        worksheet.Cell(currentRow, 7).Value = pc.LastUpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                        currentRow++;
                    }

                    currentRow++; // Add space between versions
                }

                // Auto-fit columns
                worksheet.Columns(1, 8).AdjustToContents();

                // Add borders to all cells with data
                var dataRange = worksheet.Range(1, 1, currentRow - 1, 7);
                dataRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return await Task.FromResult(stream.ToArray());
        }

        private string SanitizeSheetName(string name)
        {
            // Excel sheet names cannot contain: \ / * ? : [ ]
            // and must be <= 31 characters
            var invalidChars = new[] { '\\', '/', '*', '?', ':', '[', ']' };
            var sanitized = name;

            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            if (sanitized.Length > 31)
            {
                sanitized = sanitized.Substring(0, 31);
            }

            return sanitized;
        }
    }
}
