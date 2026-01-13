using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface ICategoryService
    {
        Task<CategoryResponse> CreateCategoryAsync(CategoryCreateRequest request);
        Task<CategoryResponse> UpdateCategoryAsync(int id, CategoryCreateRequest request);
        Task<bool> DeleteCategoryAsync(int id);
        Task<CategoryResponse?> GetCategoryByIdAsync(int id);
        Task<CategoryResponse?> GetCategoryByNameAsync(string name);
        Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync();
        Task<IEnumerable<CategoryResponse>> GetActiveCategoriesAsync();
    }
}