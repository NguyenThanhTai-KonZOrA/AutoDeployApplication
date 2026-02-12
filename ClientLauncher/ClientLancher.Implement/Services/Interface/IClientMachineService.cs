using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IClientMachineService
    {
        /// <summary>
        /// Register or update a client machine
        /// </summary>
        Task<ClientMachineResponse> RegisterMachineAsync(ClientMachineRegisterRequest request);

        /// <summary>
        /// Update heartbeat for a machine
        /// </summary>
        Task<bool> UpdateHeartbeatAsync(ClientMachineHeartbeatRequest request);

        /// <summary>
        /// Get all online machines
        /// </summary>
        Task<IEnumerable<ClientMachineResponse>> GetOnlineMachinesAsync();

        /// <summary>
        /// Get machine by ID
        /// </summary>
        Task<ClientMachineResponse?> GetMachineByIdAsync(int id);

        /// <summary>
        /// Get machine by machine ID
        /// </summary>
        Task<ClientMachineResponse?> GetMachineByMachineIdAsync(string machineId);

        /// <summary>
        /// Get all machines
        /// </summary>
        Task<IEnumerable<ClientMachineResponse>> GetAllMachinesAsync();

        /// <summary>
        /// Get machines with specific app installed
        /// </summary>
        Task<IEnumerable<ClientMachineResponse>> GetMachinesWithAppAsync(string appCode);

        /// <summary>
        /// Mark offline machines (background task)
        /// </summary>
        Task<int> MarkOfflineMachinesAsync();

        /// <summary>
        /// Get client machine statistics
        /// </summary>
        Task<object> GetStatisticsAsync();
    }
}
