using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IActiveDirectoryService
    {
        Task<ADComputerListResponse> GetAllComputersAsync(ADComputerSearchRequest? request = null);
        Task<ADComputerResponse?> GetComputerByNameAsync(string computerName);
        Task<List<ADOrganizationalUnitResponse>> GetOrganizationalUnitsAsync();
        Task<ADComputerListResponse> GetComputersInOUAsync(string ouPath);
        Task<bool> IsComputerOnlineAsync(string computerName);
        Task<List<ADComputerResponse>> SearchComputersAsync(string searchPattern);
    }
}
