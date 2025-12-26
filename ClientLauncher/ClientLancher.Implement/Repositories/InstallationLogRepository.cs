using ClientLancher.Implement.ApplicationDbContext;
using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
using ClientLancher.Implement.ViewModels.Request;
using Microsoft.EntityFrameworkCore;

namespace ClientLancher.Implement.Repositories
{
    public class InstallationLogRepository : GenericRepository<InstallationLog>, IInstallationLogRepository
    {
        public InstallationLogRepository(ClientLancherDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<InstallationLog>> GetByApplicationIdAsync(int applicationId)
        {
            return await _dbSet
                .Include(i => i.Application)
                .Where(i => i.ApplicationId == applicationId)
                .OrderByDescending(i => i.StartedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<InstallationLog>> GetByUserNameAsync(string userName)
        {
            return await _dbSet
                .Include(i => i.Application)
                .Where(i => i.UserName == userName)
                .OrderByDescending(i => i.StartedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<InstallationLog>> GetByMachineNameAsync(string machineName)
        {
            return await _dbSet
                .Include(i => i.Application)
                .Where(i => i.MachineName == machineName)
                .OrderByDescending(i => i.StartedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<InstallationLog>> GetFailedInstallationsAsync()
        {
            return await _dbSet
                .Include(i => i.Application)
                .Where(i => i.Status == "Failed")
                .OrderByDescending(i => i.StartedAt)
                .ToListAsync();
        }

        public async Task<InstallationLog?> GetLatestByAppCodeAsync(string appCode)
        {
            return await _dbSet
                .Include(i => i.Application)
                .Where(i => i.Application.AppCode == appCode)
                .OrderByDescending(i => i.StartedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<InstallationLog>> GetInstallationHistoryAsync(string appCode, int take = 10)
        {
            return await _dbSet
                .Include(i => i.Application)
                .Where(i => i.Application.AppCode == appCode)
                .OrderByDescending(i => i.StartedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<InstallationLog>> GetPaginatedInstallationLogsAsync(InstallationLogFilterRequest request)
        {
            var query = _dbSet.Include(x => x.Application).AsNoTracking().AsQueryable();

            // Apply filters dynamically
            query = ApplyFilters(query, request);

            // Calculate skip value
            int skip = request.Skip ?? (request.Page - 1) * request.PageSize;
            int take = request.Take ?? request.PageSize;

            // Apply pagination and return
            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        // Get total count with filters applied
        public async Task<int> GetFilteredCountAsync(InstallationLogFilterRequest request)
        {
            var query = _dbSet.AsQueryable();
            query = ApplyFilters(query, request);
            return await query.CountAsync();
        }

        // Helper method to apply filters
        private IQueryable<InstallationLog> ApplyFilters(IQueryable<InstallationLog> query, InstallationLogFilterRequest request)
        {
            // Filter by UserName
            if (!string.IsNullOrWhiteSpace(request.UserName))
            {
                query = query.Where(a => a.UserName.Contains(request.UserName));
            }

            // Filter by Action
            if (!string.IsNullOrWhiteSpace(request.Action))
            {
                query = query.Where(a => a.Action.Contains(request.Action));
            }

            // Filter by ApplicationId
            if (request.ApplicationId.HasValue)
            {
                query = query.Where(a => a.ApplicationId == request.ApplicationId.Value);
            }

            // Filter by Status
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(a => a.Status == request.Status);
            }

            // Filter by MachineName
            if (!string.IsNullOrWhiteSpace(request.MachineName))
            {
                query = query.Where(a => a.MachineName == request.MachineName);
            }

            // Filter by Date Range
            if (request.FromDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                // Include the entire end date (23:59:59)
                var endDate = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.CreatedAt <= endDate);
            }

            return query;
        }
    }
}