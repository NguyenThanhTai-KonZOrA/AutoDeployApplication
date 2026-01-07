using ClientLancher.Implement.EntityModels;

namespace ClientLancher.Implement.Repositories.Interface
{
    public interface IIconsRepository : IGenericRepository<Icons>
    {
        Task<IEnumerable<Icons>> GetByTypeAsync(IconType type);
        Task<Icons?> GetByTypeAndReferenceIdAsync(IconType type, int referenceId);
    }
}