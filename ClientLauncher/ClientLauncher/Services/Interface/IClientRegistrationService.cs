using System.Threading.Tasks;

namespace ClientLauncher.Services.Interface
{
    public interface IClientRegistrationService
    {
        string GetMachineId();
        Task<bool> RegisterMachineAsync();
        Task<bool> SendHeartbeatAsync();
    }
}
