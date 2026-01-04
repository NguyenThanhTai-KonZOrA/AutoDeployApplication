using ClientLancher.Implement.ApplicationDbContext;
using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLancher.Implement.Repositories
{
    public class ApplicationCategoryRepository : GenericRepository<ApplicationCategory>, IApplicationCategoryRepository
    {
        public ApplicationCategoryRepository(ClientLancherDbContext context) : base(context)
        {
        }

        public async Task<ApplicationCategory?> GetByNameAsync(string name)
        {
            return await _dbSet
                .Include(c => c.Applications)
                .FirstOrDefaultAsync(c => c.Name == name);
        }

        public async Task<IEnumerable<ApplicationCategory>> GetActiveCategoriesAsync()
        {
            return await _dbSet
                .Include(c => c.Applications)
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.DisplayName)
                .ToListAsync();
        }

        public async Task<bool> CategoryExistsAsync(string name)
        {
            return await _dbSet.AnyAsync(c => c.Name == name);
        }

        public void Delete(ApplicationCategory category)
        {
            _dbSet.Remove(category);
        }
    }
}