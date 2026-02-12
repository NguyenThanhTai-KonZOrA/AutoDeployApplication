using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.UnitOfWork;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;
using ClientLauncher.Implement.EntityModels;
using Microsoft.Extensions.Logging;

namespace ClientLauncher.Implement.Services
{
    public class DeploymentTaskService : IDeploymentTaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeploymentTaskService> _logger;

        public DeploymentTaskService(IUnitOfWork unitOfWork, ILogger<DeploymentTaskService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<DeploymentTaskResponse>> GetPendingTasksForMachineAsync(string machineId)
        {
            try
            {
                var machine = await _unitOfWork.ClientMachines.GetByMachineIdAsync(machineId);
                if (machine == null)
                {
                    _logger.LogWarning("Machine not found: {MachineId}", machineId);
                    return Enumerable.Empty<DeploymentTaskResponse>();
                }

                var tasks = await _unitOfWork.DeploymentTasks.GetPendingTasksForMachineAsync(machine.Id);
                return tasks.Select(MapToResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending tasks for machine: {MachineId}", machineId);
                throw;
            }
        }

        public async Task<bool> UpdateTaskStatusAsync(DeploymentTaskUpdateRequest request)
        {
            try
            {
                var task = await _unitOfWork.DeploymentTasks.GetByIdAsync(request.TaskId);
                if (task == null)
                {
                    _logger.LogWarning("Task not found: {TaskId}", request.TaskId);
                    return false;
                }

                // Update task based on status
                if (request.Status == "InProgress" && task.Status == "Queued")
                {
                    await _unitOfWork.DeploymentTasks.MarkAsStartedAsync(request.TaskId);
                }
                else if (request.Status == "Completed" || request.Status == "Failed")
                {
                    await _unitOfWork.DeploymentTasks.MarkAsCompletedAsync(
                        request.TaskId, 
                        request.IsSuccess, 
                        request.ErrorMessage);

                    // Update deployment history counters
                    await UpdateDeploymentHistoryCountersAsync(task.DeploymentHistoryId);
                }
                else
                {
                    // Update progress
                    await _unitOfWork.DeploymentTasks.UpdateProgressAsync(
                        request.TaskId, 
                        request.ProgressPercentage, 
                        request.CurrentStep ?? "");
                }

                // Update download size if provided
                if (request.DownloadSizeBytes.HasValue)
                {
                    task.DownloadSizeBytes = request.DownloadSizeBytes;
                    _unitOfWork.DeploymentTasks.Update(task);
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Task {TaskId} status updated to {Status}", request.TaskId, request.Status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task status: {TaskId}", request.TaskId);
                return false;
            }
        }

        public async Task<DeploymentTaskResponse?> GetTaskByIdAsync(int id)
        {
            try
            {
                var task = await _unitOfWork.DeploymentTasks.GetByIdWithDetailsAsync(id);
                return task != null ? MapToResponse(task) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task by ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<DeploymentTaskResponse>> GetTasksByDeploymentIdAsync(int deploymentHistoryId)
        {
            try
            {
                var tasks = await _unitOfWork.DeploymentTasks.GetByDeploymentHistoryIdAsync(deploymentHistoryId);
                return tasks.Select(MapToResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by deployment ID: {DeploymentId}", deploymentHistoryId);
                throw;
            }
        }

        public async Task<object> GetTaskStatisticsAsync(int? deploymentHistoryId = null)
        {
            try
            {
                return await _unitOfWork.DeploymentTasks.GetStatisticsAsync(deploymentHistoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task statistics");
                throw;
            }
        }

        public async Task<int> RetryFailedTasksAsync()
        {
            try
            {
                var retryableTasks = await _unitOfWork.DeploymentTasks.GetRetryableTasksAsync();
                int retryCount = 0;

                foreach (var task in retryableTasks)
                {
                    task.Status = "Queued";
                    task.RetryCount++;
                    task.ErrorMessage = null;
                    task.ProgressPercentage = 0;
                    task.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.DeploymentTasks.Update(task);
                    retryCount++;
                }

                if (retryCount > 0)
                {
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Retried {Count} failed tasks", retryCount);
                }

                return retryCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying failed tasks");
                throw;
            }
        }

        private async Task UpdateDeploymentHistoryCountersAsync(int deploymentHistoryId)
        {
            try
            {
                var deployment = await _unitOfWork.DeploymentHistories.GetByIdAsync(deploymentHistoryId);
                if (deployment == null) return;

                var tasks = await _unitOfWork.DeploymentTasks.GetByDeploymentHistoryIdAsync(deploymentHistoryId);
                var taskList = tasks.ToList();

                deployment.SuccessCount = taskList.Count(t => t.Status == "Completed" && t.IsSuccess);
                deployment.FailedCount = taskList.Count(t => t.Status == "Failed" || (t.Status == "Completed" && !t.IsSuccess));
                deployment.PendingCount = taskList.Count(t => t.Status == "Queued" || t.Status == "InProgress");

                // Update overall deployment status
                if (deployment.PendingCount == 0)
                {
                    if (deployment.FailedCount == 0)
                    {
                        deployment.Status = "Success";
                    }
                    else if (deployment.SuccessCount == 0)
                    {
                        deployment.Status = "Failed";
                    }
                    else
                    {
                        deployment.Status = "Partial Success";
                    }
                    deployment.CompletedAt = DateTime.UtcNow;
                }
                else if (deployment.SuccessCount > 0 || deployment.FailedCount > 0)
                {
                    deployment.Status = "InProgress";
                }

                _unitOfWork.DeploymentHistories.Update(deployment);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deployment history counters");
            }
        }

        private DeploymentTaskResponse MapToResponse(DeploymentTask task)
        {
            return new DeploymentTaskResponse
            {
                Id = task.Id,
                DeploymentHistoryId = task.DeploymentHistoryId,
                TargetMachineId = task.TargetMachineId,
                TargetMachineName = task.TargetMachine?.MachineName ?? "",
                AppCode = task.AppCode,
                AppName = task.AppName,
                Version = task.Version,
                Status = task.Status,
                Priority = task.Priority,
                ProgressPercentage = task.ProgressPercentage,
                CurrentStep = task.CurrentStep,
                CreatedAt = task.CreatedAt,
                ScheduledFor = task.ScheduledFor,
                StartedAt = task.StartedAt,
                CompletedAt = task.CompletedAt,
                IsSuccess = task.IsSuccess,
                ErrorMessage = task.ErrorMessage,
                RetryCount = task.RetryCount,
                InstallDuration = task.InstallDuration
            };
        }
    }
}