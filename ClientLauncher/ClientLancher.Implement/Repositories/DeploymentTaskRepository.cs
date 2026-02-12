using ClientLauncher.Implement.ApplicationDbContext;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLauncher.Implement.Repositories
{
    public class DeploymentTaskRepository : GenericRepository<DeploymentTask>, IDeploymentTaskRepository
    {
        public DeploymentTaskRepository(DeploymentManagerDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<DeploymentTask>> GetPendingTasksForMachineAsync(int machineId)
        {
            var now = DateTime.UtcNow;
            
            return await _dbSet
                .Include(t => t.PackageVersion)
                    .ThenInclude(p => p.Application)
                .Include(t => t.DeploymentHistory)
                .Where(t => t.TargetMachineId == machineId && 
                           (t.Status == "Queued" || t.Status == "InProgress") &&
                           (t.ScheduledFor == null || t.ScheduledFor <= now))
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DeploymentTask>> GetByDeploymentHistoryIdAsync(int deploymentHistoryId)
        {
            return await _dbSet
                .Include(t => t.TargetMachine)
                .Include(t => t.PackageVersion)
                .Where(t => t.DeploymentHistoryId == deploymentHistoryId)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DeploymentTask>> GetByStatusAsync(string status)
        {
            return await _dbSet
                .Include(t => t.TargetMachine)
                .Include(t => t.PackageVersion)
                .Include(t => t.DeploymentHistory)
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DeploymentTask>> GetRetryableTasksAsync()
        {
            var now = DateTime.UtcNow;
            
            return await _dbSet
                .Include(t => t.PackageVersion)
                .Include(t => t.TargetMachine)
                .Where(t => t.Status == "Failed" && 
                           t.RetryCount < t.MaxRetries &&
                           (t.NextRetryAt == null || t.NextRetryAt <= now))
                .OrderBy(t => t.NextRetryAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DeploymentTask>> GetScheduledTasksReadyToExecuteAsync()
        {
            var now = DateTime.UtcNow;
            
            return await _dbSet
                .Include(t => t.PackageVersion)
                .Include(t => t.TargetMachine)
                .Where(t => t.Status == "Queued" && 
                           t.ScheduledFor != null && 
                           t.ScheduledFor <= now)
                .OrderBy(t => t.ScheduledFor)
                .ToListAsync();
        }

        public async Task<DeploymentTask?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(t => t.TargetMachine)
                .Include(t => t.PackageVersion)
                    .ThenInclude(p => p.Application)
                .Include(t => t.DeploymentHistory)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<bool> UpdateProgressAsync(int taskId, int percentage, string currentStep)
        {
            var task = await _dbSet.FindAsync(taskId);
            if (task == null)
                return false;

            task.ProgressPercentage = percentage;
            task.CurrentStep = currentStep;
            task.UpdatedAt = DateTime.UtcNow;

            _context.Update(task);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> MarkAsStartedAsync(int taskId)
        {
            var task = await _dbSet.FindAsync(taskId);
            if (task == null)
                return false;

            task.Status = "InProgress";
            task.StartedAt = DateTime.UtcNow;
            task.ProgressPercentage = 0;
            task.UpdatedAt = DateTime.UtcNow;

            _context.Update(task);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> MarkAsCompletedAsync(int taskId, bool isSuccess, string? errorMessage = null)
        {
            var task = await _dbSet.FindAsync(taskId);
            if (task == null)
                return false;

            task.Status = isSuccess ? "Completed" : "Failed";
            task.IsSuccess = isSuccess;
            task.CompletedAt = DateTime.UtcNow;
            task.ErrorMessage = errorMessage;
            task.ProgressPercentage = isSuccess ? 100 : task.ProgressPercentage;
            task.UpdatedAt = DateTime.UtcNow;

            if (task.StartedAt.HasValue)
            {
                task.InstallDuration = DateTime.UtcNow - task.StartedAt.Value;
            }

            // Set retry if failed and retries available
            if (!isSuccess && task.RetryCount < task.MaxRetries)
            {
                task.NextRetryAt = DateTime.UtcNow.AddMinutes(5 * (task.RetryCount + 1)); // Exponential backoff
            }

            _context.Update(task);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<DeploymentTaskStatistics> GetStatisticsAsync(int? deploymentHistoryId = null)
        {
            var query = _dbSet.AsQueryable();
            
            if (deploymentHistoryId.HasValue)
            {
                query = query.Where(t => t.DeploymentHistoryId == deploymentHistoryId.Value);
            }

            var tasks = await query.ToListAsync();
            
            var completed = tasks.Where(t => t.Status == "Completed").ToList();
            var totalCompleted = tasks.Count(t => t.Status == "Completed" || t.Status == "Failed");
            
            return new DeploymentTaskStatistics
            {
                TotalTasks = tasks.Count,
                QueuedTasks = tasks.Count(t => t.Status == "Queued"),
                InProgressTasks = tasks.Count(t => t.Status == "InProgress"),
                CompletedTasks = tasks.Count(t => t.Status == "Completed"),
                FailedTasks = tasks.Count(t => t.Status == "Failed"),
                CancelledTasks = tasks.Count(t => t.Status == "Cancelled"),
                SuccessRate = totalCompleted > 0 ? (double)completed.Count / totalCompleted * 100 : 0,
                AverageInstallDuration = completed.Any(t => t.InstallDuration.HasValue)
                    ? TimeSpan.FromTicks((long)completed.Where(t => t.InstallDuration.HasValue)
                        .Average(t => t.InstallDuration!.Value.Ticks))
                    : null
            };
        }
    }
}
