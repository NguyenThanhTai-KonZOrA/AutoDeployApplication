using ClientLancher.Implement.ApplicationDbContext;
using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
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
    }
}