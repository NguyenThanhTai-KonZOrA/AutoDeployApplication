using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.Repositories.Interface
{
    public interface IClientMachineRepository : IGenericRepository<ClientMachine>
    {
        /// <summary>
        /// Get client machine by unique machine ID
        /// </summary>
        Task<ClientMachine?> GetByMachineIdAsync(string machineId);

        /// <summary>
        /// Get all online client machines (based on heartbeat threshold)
        /// </summary>
        Task<IEnumerable<ClientMachine>> GetOnlineMachinesAsync(int heartbeatThresholdMinutes = 2);

        /// <summary>
        /// Get client machines by status
        /// </summary>
        Task<IEnumerable<ClientMachine>> GetByStatusAsync(string status);

        /// <summary>
        /// Get client machines by username
        /// </summary>
        Task<IEnumerable<ClientMachine>> GetByUserNameAsync(string userName);

        /// <summary>
        /// Get client machines that have a specific app installed
        /// </summary>
        Task<IEnumerable<ClientMachine>> GetMachinesWithAppInstalledAsync(string appCode);

        /// <summary>
        /// Update heartbeat timestamp and status
        /// </summary>
        Task<bool> UpdateHeartbeatAsync(string machineId);

        /// <summary>
        /// Mark offline machines based on heartbeat threshold
        /// </summary>
        Task<int> MarkOfflineMachinesAsync(int heartbeatThresholdMinutes = 2);

        /// <summary>
        /// Get client machine statistics
        /// </summary>
        Task<ClientMachineStatistics> GetStatisticsAsync();
    }

    public class ClientMachineStatistics
    {
        public int TotalMachines { get; set; }
        public int OnlineMachines { get; set; }
        public int OfflineMachines { get; set; }
        public int BusyMachines { get; set; }
        public DateTime? LastRegistration { get; set; }
    }
}
