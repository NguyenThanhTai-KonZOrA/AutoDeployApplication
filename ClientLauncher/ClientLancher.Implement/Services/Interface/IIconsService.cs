using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.AspNetCore.Http;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IIconsService
    {
        Task<IconResponse?> GetByIdAsync(int id);
        Task<IEnumerable<IconResponse>> GetAllAsync();
        Task<IEnumerable<IconResponse>> GetByTypeAsync(IconType type);
        Task<IconResponse?> GetByTypeAndReferenceIdAsync(IconType type, int referenceId);
        Task<IconResponse> CreateAsync(CreateIconRequest createDto, string createdBy);
        Task<IconResponse?> UpdateAsync(int id, UpdateIconRequest updateDto, string updatedBy);
        Task<IconResponse?> UploadIconAsync(int id, IFormFile file, string updatedBy);
        Task<bool> DeleteAsync(int id, string deletedBy);
    }
}