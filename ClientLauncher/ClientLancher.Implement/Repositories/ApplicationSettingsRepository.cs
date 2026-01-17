using ClientLauncher.Implement.ApplicationDbContext;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLauncher.Implement.Repositories
{
    public class ApplicationSettingsRepository : GenericRepository<ApplicationSettings>, IApplicationSettingsRepository
    {
        private readonly DeploymentManagerDbContext _context;

        public ApplicationSettingsRepository(DeploymentManagerDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ApplicationSettings?> GetByKeyAsync(string key)
        {
            return await _context.ApplicationSettings
                .FirstOrDefaultAsync(s => s.Key == key);
        }

        public async Task<List<ApplicationSettings>> GetByCategoryAsync(string category)
        {
            return await _context.ApplicationSettings
                .Where(s => s.Category == category)
                .OrderBy(s => s.Key)
                .ToListAsync();
        }

        public async Task<bool> UpdateSettingAsync(string key, string value, string updatedBy)
        {
            var setting = await GetByKeyAsync(key);
            if (setting == null) return false;

            setting.Value = value;
            setting.UpdatedBy = updatedBy;
            setting.UpdatedAt = DateTime.UtcNow;

            _context.ApplicationSettings.Update(setting);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<ApplicationSettings>> GetAllApplicationSettingsAsync()
        {
            return await _context.ApplicationSettings.OrderByDescending(s => s.Category).AsNoTracking().ToListAsync();
        }
    }
}