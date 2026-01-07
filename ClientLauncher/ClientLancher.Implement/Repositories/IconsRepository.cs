using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLancher.Implement.Repositories
{
    public class IconsRepository : GenericRepository<Icons>, IIconsRepository
    {
        public IconsRepository(ApplicationDbContext.ClientLancherDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Icons>> GetByTypeAsync(IconType type)
        {
            return await _context.Icons
                .Where(i => i.Type == type && i.IsActive && !i.IsDelete)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<Icons?> GetByTypeAndReferenceIdAsync(IconType type, int referenceId)
        {
            return await _context.Icons
                .FirstOrDefaultAsync(i => i.Type == type
                    && i.ReferenceId == referenceId
                    && i.IsActive
                    && !i.IsDelete);
        }
    }
}