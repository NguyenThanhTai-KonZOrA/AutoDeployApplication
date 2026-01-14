using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.Repositories.Interface
{
    public interface IApplicationCategoryRepository : IGenericRepository<ApplicationCategory>
    {
        Task<ApplicationCategory?> GetByNameAsync(string name);
        Task<IEnumerable<ApplicationCategory>> GetActiveCategoriesAsync();
        Task<bool> CategoryExistsAsync(string name);
        void Delete(ApplicationCategory category);
    }
}