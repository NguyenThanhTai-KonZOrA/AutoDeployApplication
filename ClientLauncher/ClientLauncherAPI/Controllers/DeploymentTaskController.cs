using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeploymentTaskController : ControllerBase
    {
        private readonly IDeploymentTaskService _deploymentTaskService;
        private readonly ILogger<DeploymentTaskController> _logger;

        public DeploymentTaskController(
            IDeploymentTaskService deploymentTaskService,
            ILogger<DeploymentTaskController> logger)
        {
            _deploymentTaskService = deploymentTaskService;
            _logger = logger;
        }

        /// <summary>
        /// Get pending tasks for a machine (for client polling)
        /// </summary>
        [HttpGet("pending/{machineId}")]
        public async Task<IActionResult> GetPendingTasks(string machineId)
        {
            try
            {
                var result = await _deploymentTaskService.GetPendingTasksForMachineAsync(machineId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending tasks for machine: {MachineId}", machineId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update task status (for client to report progress)
        /// </summary>
        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateTaskStatus([FromBody] DeploymentTaskUpdateRequest request)
        {
            try
            {
                var result = await _deploymentTaskService.UpdateTaskStatusAsync(request);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Task not found" });
                }
                return Ok(new { success = true, message = "Task status updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task status: {TaskId}", request.TaskId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get task by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            try
            {
                var result = await _deploymentTaskService.GetTaskByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Task not found" });
                }
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task by ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get tasks by deployment history ID
        /// </summary>
        [HttpGet("by-deployment/{deploymentId}")]
        public async Task<IActionResult> GetTasksByDeploymentId(int deploymentId)
        {
            try
            {
                var result = await _deploymentTaskService.GetTasksByDeploymentIdAsync(deploymentId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by deployment ID: {DeploymentId}", deploymentId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get task statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] int? deploymentId = null)
        {
            try
            {
                var result = await _deploymentTaskService.GetTaskStatisticsAsync(deploymentId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task statistics");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Retry failed tasks
        /// </summary>
        [HttpPost("retry-failed")]
        public async Task<IActionResult> RetryFailedTasks()
        {
            try
            {
                var count = await _deploymentTaskService.RetryFailedTasksAsync();
                return Ok(new { success = true, data = new { retriedCount = count }, message = $"Retried {count} failed tasks" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying failed tasks");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}
