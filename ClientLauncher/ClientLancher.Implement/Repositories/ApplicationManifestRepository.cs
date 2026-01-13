using ClientLauncher.Implement.ApplicationDbContext;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLauncher.Implement.Repositories
{
    public class ApplicationManifestRepository : GenericRepository<ApplicationManifest>, IApplicationManifestRepository
    {
        public ApplicationManifestRepository(DeploymentManagerDbContext context) : base(context)
        {
        }

        public async Task<ApplicationManifest?> GetLatestActiveManifestAsync(int applicationId)
        {
            return await _dbSet
                .Include(m => m.Application)
                .Where(m => m.ApplicationId == applicationId && m.IsActive)
                .OrderByDescending(m => m.PublishedAt ?? m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<ApplicationManifest?> GetLatestActiveManifestByAppCodeAsync(string appCode)
        {
            return await _dbSet
                .Include(m => m.Application)
                .Where(m => m.Application.AppCode == appCode && m.IsActive)
                .OrderByDescending(m => m.PublishedAt ?? m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<ApplicationManifest?> GetByVersionAsync(int applicationId, string version)
        {
            return await _dbSet
                .Include(m => m.Application)
                .FirstOrDefaultAsync(m => m.ApplicationId == applicationId && m.Version == version);
        }

        public async Task<IEnumerable<ApplicationManifest>> GetManifestHistoryAsync(int applicationId, int take = 10)
        {
            return await _dbSet
                .Include(m => m.Application)
                .Where(m => m.ApplicationId == applicationId)
                .OrderByDescending(m => m.PublishedAt ?? m.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> VersionExistsAsync(int applicationId, string version)
        {
            return await _dbSet.AnyAsync(m => m.ApplicationId == applicationId && m.Version == version);
        }

        public async Task DeactivateAllManifestsAsync(int applicationId)
        {
            var manifests = await _dbSet
                .Where(m => m.ApplicationId == applicationId && m.IsActive)
                .ToListAsync();

            foreach (var manifest in manifests)
            {
                manifest.IsActive = false;
                manifest.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}