using ClientLancher.Implement.ApplicationDbContext;
using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLancher.Implement.Repositories
{
    public class ApplicationRepository : GenericRepository<Application>, IApplicationRepository
    {
        public ApplicationRepository(ClientLancherDbContext context) : base(context)
        {
        }

        public async Task<Application?> GetByAppCodeAsync(string appCode)
        {
            return await _dbSet
                .Include(a => a.InstallationLogs)
                .FirstOrDefaultAsync(a => a.AppCode == appCode);
        }

        public async Task<IEnumerable<Application>> GetActiveApplicationsAsync()
        {
            return await _dbSet
                .Where(a => a.IsActive)
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetApplicationsByCategoryAsync(string category)
        {
            return await _dbSet
                .Where(a => a.IsActive && a.Category == category)
                .OrderBy(a => a.Name)
                .ToListAsync();
        }
    }
}