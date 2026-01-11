using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;

namespace ClientLancher.Implement.Services.Interface
{
    public interface IAuditLogService
    {
        Task<AuditLogPaginationResponse> GetPaginatedLogsAsync(AuditLogPaginationRequest request);
        Task LogActionAsync(CreateAuditLogRequest request);
        Task<AuditLogResponse?> GetByIdAsync(int id);
        Task<AuditLogPagedResponse> GetAllAsync(int page = 1, int pageSize = 50);
        Task<AuditLogPagedResponse> GetByUserNameAsync(string userName, int page = 1, int pageSize = 50);
        Task<AuditLogPagedResponse> GetByActionAsync(string action, int page = 1, int pageSize = 50);
        Task<AuditLogPagedResponse> GetByEntityAsync(string entityType, int entityId, int page = 1, int pageSize = 50);
        Task<AuditLogPagedResponse> GetFailedLogsAsync(int page = 1, int pageSize = 50);
        Task<AuditLogPagedResponse> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 50);
    }
}