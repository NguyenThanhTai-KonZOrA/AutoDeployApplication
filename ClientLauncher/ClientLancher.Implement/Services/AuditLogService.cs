using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;

namespace ClientLancher.Implement.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(IAuditLogRepository auditLogRepository, ILogger<AuditLogService> logger)
        {
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated audit logs with multiple filter conditions
        /// </summary>
        public async Task<AuditLogPaginationResponse> GetPaginatedLogsAsync(AuditLogPaginationRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "[GetPaginatedLogsAsync] START - Page: {Page}, PageSize: {PageSize}, UserName: {UserName}, Action: {Action}, EntityType: {EntityType}, IsSuccess: {IsSuccess}, FromDate: {FromDate}, ToDate: {ToDate}",
                    request.Page, request.PageSize, request.UserName, request.Action, request.EntityType,
                    request.IsSuccess, request.FromDate, request.ToDate
                );

                // Get filtered and paginated logs
                var logs = await _auditLogRepository.GetPaginatedLogsAsync(request);

                // Get total count with same filters applied
                var totalRecords = await _auditLogRepository.GetFilteredCountAsync(request);
                var totalUsedApplications = await _auditLogRepository.GetTotalUsedApplicationsAsync();

                _logger.LogInformation(
                    "[GetPaginatedLogsAsync] END - Retrieved {Count} logs out of {Total} total records",
                    logs.Count, totalRecords
                );

                return new AuditLogPaginationResponse
                {
                    Logs = logs.Select(MapToResponse).ToList(),
                    TotalRecords = totalRecords,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalUsedApplications = totalUsedApplications
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetPaginatedLogsAsync] FAILED");
                throw;
            }
        }

        public async Task LogActionAsync(CreateAuditLogRequest request)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserName = request.UserName,
                    Action = request.Action,
                    EntityType = request.EntityType,
                    EntityId = request.EntityId,
                    HttpMethod = request.HttpMethod,
                    RequestPath = request.RequestPath,
                    IpAddress = request.IpAddress,
                    UserAgent = request.UserAgent,
                    IsSuccess = request.IsSuccess,
                    StatusCode = request.StatusCode,
                    ErrorMessage = request.ErrorMessage,
                    Details = request.Details,
                    DurationMs = request.DurationMs,
                    CreatedBy = request.UserName
                };

                await _auditLogRepository.AddAsync(auditLog);
                _logger.LogInformation("[AuditLog] Logged action: {Action} by {UserName}", request.Action, request.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuditLog] Failed to log action: {Action}", request.Action);
            }
        }

        public async Task<AuditLogResponse?> GetByIdAsync(int id)
        {
            var log = await _auditLogRepository.GetByIdAsync(id);
            if (log == null) return null;

            return MapToResponse(log);
        }

        public async Task<AuditLogPagedResponse> GetAllAsync(int page = 1, int pageSize = 50)
        {
            var logs = await _auditLogRepository.GetAllAsync(page, pageSize);
            var totalCount = await _auditLogRepository.GetTotalCountAsync();

            return new AuditLogPagedResponse
            {
                Logs = logs.Select(MapToResponse).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AuditLogPagedResponse> GetByUserNameAsync(string userName, int page = 1, int pageSize = 50)
        {
            var logs = await _auditLogRepository.GetByUserNameAsync(userName, page, pageSize);
            var totalCount = await _auditLogRepository.GetTotalCountAsync();

            return new AuditLogPagedResponse
            {
                Logs = logs.Select(MapToResponse).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AuditLogPagedResponse> GetByActionAsync(string action, int page = 1, int pageSize = 50)
        {
            var logs = await _auditLogRepository.GetByActionAsync(action, page, pageSize);
            var totalCount = await _auditLogRepository.GetTotalCountAsync();

            return new AuditLogPagedResponse
            {
                Logs = logs.Select(MapToResponse).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AuditLogPagedResponse> GetByEntityAsync(string entityType, int entityId, int page = 1, int pageSize = 50)
        {
            var logs = await _auditLogRepository.GetByEntityAsync(entityType, entityId, page, pageSize);
            var totalCount = await _auditLogRepository.GetTotalCountAsync();

            return new AuditLogPagedResponse
            {
                Logs = logs.Select(MapToResponse).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AuditLogPagedResponse> GetFailedLogsAsync(int page = 1, int pageSize = 50)
        {
            var logs = await _auditLogRepository.GetFailedLogsAsync(page, pageSize);
            var totalCount = await _auditLogRepository.GetTotalCountAsync();

            return new AuditLogPagedResponse
            {
                Logs = logs.Select(MapToResponse).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AuditLogPagedResponse> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 50)
        {
            var logs = await _auditLogRepository.GetByDateRangeAsync(startDate, endDate, page, pageSize);
            var totalCount = await _auditLogRepository.GetTotalCountAsync();

            return new AuditLogPagedResponse
            {
                Logs = logs.Select(MapToResponse).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private AuditLogResponse MapToResponse(AuditLog log)
        {
            return new AuditLogResponse
            {
                ID = log.ID,
                UserName = log.UserName,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                HttpMethod = log.HttpMethod,
                RequestPath = log.RequestPath,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                IsSuccess = log.IsSuccess,
                StatusCode = log.StatusCode,
                ErrorMessage = log.ErrorMessage,
                Details = log.Details,
                DurationMs = log.DurationMs,
                CreatedAt = log.CreatedAt
            };
        }
    }
}
