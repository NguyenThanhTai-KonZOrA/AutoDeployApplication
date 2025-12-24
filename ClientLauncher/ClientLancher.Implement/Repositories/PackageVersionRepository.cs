using ClientLancher.Implement.ApplicationDbContext;
using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLancher.Implement.Repositories
{
    public class PackageVersionRepository : GenericRepository<PackageVersion>, IPackageVersionRepository
    {
        public PackageVersionRepository(ClientLancherDbContext context) : base(context)
        {
        }

        public async Task<PackageVersion?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Application)
                .Include(p => p.ReplacesVersion)
                .Include(p => p.DeploymentHistories)
                .Include(p => p.DownloadStatistics)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<PackageVersion>> GetByApplicationIdAsync(int applicationId)
        {
            return await _dbSet
                .Include(p => p.Application)
                .Where(p => p.ApplicationId == applicationId)
                .OrderByDescending(p => p.UploadedAt)
                .ToListAsync();
        }

        public async Task<PackageVersion?> GetByApplicationAndVersionAsync(int applicationId, string version)
        {
            return await _dbSet
                .Include(p => p.Application)
                .FirstOrDefaultAsync(p => p.ApplicationId == applicationId && p.Version == version);
        }

        public async Task<PackageVersion?> GetLatestVersionAsync(int applicationId, bool stableOnly = true)
        {
            var query = _dbSet
                .Where(p => p.ApplicationId == applicationId && p.IsActive);

            if (stableOnly)
            {
                query = query.Where(p => p.IsStable);
            }

            return await query
                .OrderByDescending(p => p.PublishedAt ?? p.UploadedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<PackageVersion>> GetVersionHistoryAsync(int applicationId, int take = 10)
        {
            return await _dbSet
                .Include(p => p.Application)
                .Where(p => p.ApplicationId == applicationId)
                .OrderByDescending(p => p.UploadedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<PackageVersion>> GetActiveVersionsAsync(int applicationId)
        {
            return await _dbSet
                .Where(p => p.ApplicationId == applicationId && p.IsActive)
                .OrderByDescending(p => p.UploadedAt)
                .ToListAsync();
        }

        public async Task<bool> VersionExistsAsync(int applicationId, string version)
        {
            return await _dbSet
                .AnyAsync(p => p.ApplicationId == applicationId && p.Version == version);
        }

        public async Task<IEnumerable<PackageVersion>> GetPendingPublishAsync()
        {
            return await _dbSet
                .Include(p => p.Application)
                .Where(p => p.PublishedAt == null && p.IsActive)
                .OrderBy(p => p.UploadedAt)
                .ToListAsync();
        }

        public async Task<long> GetTotalStorageSizeAsync(int? applicationId = null)
        {
            var query = _dbSet.AsQueryable();

            if (applicationId.HasValue)
            {
                query = query.Where(p => p.ApplicationId == applicationId.Value);
            }

            return await query.SumAsync(p => p.FileSizeBytes);
        }

        public void Delete(PackageVersion package)
        {
            _dbSet.Remove(package);

            _context.SaveChanges();
        }
    }
}