using ClientLancher.Implement.ApplicationDbContext;
using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLancher.Implement.Repositories
{
    public class DownloadStatisticRepository : GenericRepository<DownloadStatistic>, IDownloadStatisticRepository
    {
        public DownloadStatisticRepository(ClientLancherDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<DownloadStatistic>> GetByPackageVersionIdAsync(int packageVersionId)
        {
            return await _dbSet
                .Include(d => d.PackageVersion)
                .Where(d => d.PackageVersionId == packageVersionId)
                .OrderByDescending(d => d.DownloadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DownloadStatistic>> GetByMachineAsync(string machineName)
        {
            return await _dbSet
                .Include(d => d.PackageVersion)
                    .ThenInclude(p => p.Application)
                .Where(d => d.MachineName == machineName)
                .OrderByDescending(d => d.DownloadedAt)
                .ToListAsync();
        }

        public async Task<int> GetTotalDownloadsAsync(int packageVersionId)
        {
            return await _dbSet
                .CountAsync(d => d.PackageVersionId == packageVersionId && d.Success);
        }

        public async Task<IEnumerable<DownloadStatistic>> GetRecentDownloadsAsync(int take = 50)
        {
            return await _dbSet
                .Include(d => d.PackageVersion)
                    .ThenInclude(p => p.Application)
                .OrderByDescending(d => d.DownloadedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetDownloadsByDateAsync(int packageVersionId, int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var downloads = await _dbSet
                .Where(d => d.PackageVersionId == packageVersionId &&
                            d.DownloadedAt >= startDate &&
                            d.Success)
                .GroupBy(d => d.DownloadedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return downloads.ToDictionary(
                x => x.Date.ToString("yyyy-MM-dd"),
                x => x.Count
            );
        }
    }
}