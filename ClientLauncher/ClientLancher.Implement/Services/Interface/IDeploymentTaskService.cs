using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IDeploymentTaskService
    {
        /// <summary>
        /// Get pending tasks for a machine (for client polling)
        /// </summary>
        Task<IEnumerable<DeploymentTaskResponse>> GetPendingTasksForMachineAsync(string machineId);

        /// <summary>
        /// Update task progress/status
        /// </summary>
        Task<bool> UpdateTaskStatusAsync(DeploymentTaskUpdateRequest request);

        /// <summary>
        /// Get task by ID
        /// </summary>
        Task<DeploymentTaskResponse?> GetTaskByIdAsync(int id);

        /// <summary>
        /// Get tasks by deployment history ID
        /// </summary>
        Task<IEnumerable<DeploymentTaskResponse>> GetTasksByDeploymentIdAsync(int deploymentHistoryId);

        /// <summary>
        /// Get task statistics
        /// </summary>
        Task<object> GetTaskStatisticsAsync(int? deploymentHistoryId = null);

        /// <summary>
        /// Retry failed tasks
        /// </summary>
        Task<int> RetryFailedTasksAsync();
    }
}
