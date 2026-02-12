using ClientLauncher.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClientLauncher.Services.Interface
{
    public interface IDeploymentPollingService
    {
        Task<List<DeploymentTaskDto>> GetPendingTasksAsync();
        Task ProcessPendingTasksAsync();
        Task<bool> UpdateTaskStatusAsync(
            int taskId, 
            string status, 
            int progressPercentage, 
            string? currentStep = null,
            bool isSuccess = false, 
            string? errorMessage = null,
            long? downloadSizeBytes = null);
    }
}
