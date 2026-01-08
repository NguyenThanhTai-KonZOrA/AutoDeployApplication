using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.UnitOfWork;
using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;

namespace ClientLancher.Implement.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CategoryService> _logger;
        private readonly IApplicationCategoryRepository _categoryRepository;

        public CategoryService(IUnitOfWork unitOfWork, ILogger<CategoryService> logger, IApplicationCategoryRepository categoryRepository)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _categoryRepository = categoryRepository;
        }

        public async Task<CategoryResponse> CreateCategoryAsync(CategoryCreateRequest request)
        {
            try
            {
                _logger.LogInformation("Creating category: {Name}", request.Name);

                if (await _unitOfWork.ApplicationCategories.CategoryExistsAsync(request.Name))
                {
                    throw new Exception($"Category '{request.Name}' already exists");
                }

                var category = new ApplicationCategory
                {
                    Name = request.Name,
                    DisplayName = request.DisplayName,
                    Description = request.Description,
                    IconUrl = request.IconUrl,
                    DisplayOrder = request.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.ApplicationCategories.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Category created: ID {Id}", category.Id);

                return MapToResponse(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                throw;
            }
        }

        public async Task<CategoryResponse> UpdateCategoryAsync(int id, CategoryCreateRequest request)
        {
            try
            {
                var category = await _unitOfWork.ApplicationCategories.GetByIdAsync(id);
                if (category == null)
                {
                    throw new Exception($"Category with ID {id} not found");
                }

                category.Name = request.Name;
                category.DisplayName = request.DisplayName;
                category.Description = request.Description;
                category.IconUrl = request.IconUrl;
                category.DisplayOrder = request.DisplayOrder;

                _unitOfWork.ApplicationCategories.Update(category);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Category updated: ID {Id}", id);

                return MapToResponse(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _unitOfWork.ApplicationCategories.GetByIdAsync(id);
                if (category == null)
                {
                    return false;
                }

                // Check if has applications
                var apps = await _unitOfWork.Applications.GetApplicationsByCategoryAsync(category.Name);
                if (apps.Any())
                {
                    throw new Exception("Cannot delete category with existing applications");
                }

                _unitOfWork.ApplicationCategories.Delete(category);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Category deleted: ID {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category ID: {Id}", id);
                throw;
            }
        }

        public async Task<CategoryResponse?> GetCategoryByIdAsync(int id)
        {
            var category = await _unitOfWork.ApplicationCategories.GetByIdAsync(id);
            return category != null ? MapToResponse(category) : null;
        }

        public async Task<CategoryResponse?> GetCategoryByNameAsync(string name)
        {
            var category = await _unitOfWork.ApplicationCategories.GetByNameAsync(name);
            return category != null ? MapToResponse(category) : null;
        }

        public async Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetActiveCategoriesAsync();
            return categories.Select(MapToResponse);
        }

        public async Task<IEnumerable<CategoryResponse>> GetActiveCategoriesAsync()
        {
            var categories = await _unitOfWork.ApplicationCategories.GetActiveCategoriesAsync();
            return categories.Select(MapToResponse);
        }

        private CategoryResponse MapToResponse(ApplicationCategory category)
        {
            return new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                DisplayName = category.DisplayName,
                Description = category.Description,
                IconUrl = category.IconUrl,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                ApplicationCount = category.Applications?.Count ?? 0,
                UpdatedAt = category.UpdatedAt
            };
        }
    }
}