using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.Repositories.Interface
{
    public interface IDeploymentTaskRepository : IGenericRepository<DeploymentTask>
    {
        /// <summary>
        /// Get pending tasks for a specific machine
        /// </summary>
        Task<IEnumerable<DeploymentTask>> GetPendingTasksForMachineAsync(int machineId);

        /// <summary>
        /// Get tasks by deployment history ID
        /// </summary>
        Task<IEnumerable<DeploymentTask>> GetByDeploymentHistoryIdAsync(int deploymentHistoryId);

        /// <summary>
        /// Get tasks by status
        /// </summary>
        Task<IEnumerable<DeploymentTask>> GetByStatusAsync(string status);

        /// <summary>
        /// Get failed tasks that can be retried
        /// </summary>
        Task<IEnumerable<DeploymentTask>> GetRetryableTasksAsync();

        /// <summary>
        /// Get scheduled tasks that are ready to execute
        /// </summary>
        Task<IEnumerable<DeploymentTask>> GetScheduledTasksReadyToExecuteAsync();

        /// <summary>
        /// Get task by ID with full details (includes navigation properties)
        /// </summary>
        Task<DeploymentTask?> GetByIdWithDetailsAsync(int id);

        /// <summary>
        /// Update task progress
        /// </summary>
        Task<bool> UpdateProgressAsync(int taskId, int percentage, string currentStep);

        /// <summary>
        /// Mark task as started
        /// </summary>
        Task<bool> MarkAsStartedAsync(int taskId);

        /// <summary>
        /// Mark task as completed
        /// </summary>
        Task<bool> MarkAsCompletedAsync(int taskId, bool isSuccess, string? errorMessage = null);

        /// <summary>
        /// Get deployment task statistics
        /// </summary>
        Task<DeploymentTaskStatistics> GetStatisticsAsync(int? deploymentHistoryId = null);
    }

    public class DeploymentTaskStatistics
    {
        public int TotalTasks { get; set; }
        public int QueuedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int FailedTasks { get; set; }
        public int CancelledTasks { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan? AverageInstallDuration { get; set; }
    }
}
