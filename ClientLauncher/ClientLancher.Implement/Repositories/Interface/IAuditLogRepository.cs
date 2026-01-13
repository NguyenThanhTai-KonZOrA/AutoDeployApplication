using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.ViewModels.Request;

namespace ClientLauncher.Implement.Repositories.Interface
{
    public interface IAuditLogRepository
    {
        Task<AuditLog> AddAsync(AuditLog auditLog);
        Task<AuditLog?> GetByIdAsync(int id);
        Task<List<AuditLog>> GetAllAsync(int page, int pageSize);
        Task<List<AuditLog>> GetByUserNameAsync(string userName, int page, int pageSize);
        Task<List<AuditLog>> GetByActionAsync(string action, int page, int pageSize);
        Task<List<AuditLog>> GetByEntityAsync(string entityType, int entityId, int page, int pageSize);
        Task<List<AuditLog>> GetFailedLogsAsync(int page, int pageSize);
        Task<List<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page, int pageSize);
        Task<int> GetTotalCountAsync();
        Task<List<AuditLog>> GetPaginatedLogsAsync(AuditLogPaginationRequest request);
        Task<int> GetFilteredCountAsync(AuditLogPaginationRequest request);
        Task<int> GetTotalUsedApplicationsAsync();
    }
}