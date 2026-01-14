using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeploymentController : ControllerBase
    {
        private readonly IDeploymentService _deploymentService;
        private readonly ILogger<DeploymentController> _logger;

        public DeploymentController(
            IDeploymentService deploymentService,
            ILogger<DeploymentController> logger)
        {
            _deploymentService = deploymentService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new deployment
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDeployment([FromBody] DeploymentCreateRequest request)
        {
            try
            {
                var result = await _deploymentService.CreateDeploymentAsync(request);
                return CreatedAtAction(nameof(GetDeployment), new { id = result.Id },
                    new { success = true, data = result, message = "Deployment created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deployment");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get deployment by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDeployment(int id)
        {
            try
            {
                var result = await _deploymentService.GetDeploymentByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Deployment not found" });
                }
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deployment ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all deployments
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllDeployments()
        {
            try
            {
                var result = await _deploymentService.GetAllDeploymentsAsync();
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all deployments");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get pending deployments
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingDeployments()
        {
            try
            {
                var result = await _deploymentService.GetPendingDeploymentsAsync();
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending deployments");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get deployments by environment
        /// </summary>
        [HttpGet("environment/{environment}")]
        public async Task<IActionResult> GetDeploymentsByEnvironment(string environment)
        {
            try
            {
                var result = await _deploymentService.GetDeploymentsByEnvironmentAsync(environment);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deployments for environment: {Environment}", environment);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Start a deployment
        /// </summary>
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartDeployment(int id)
        {
            try
            {
                var result = await _deploymentService.StartDeploymentAsync(id);
                return Ok(new { success = true, data = result, message = "Deployment started" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting deployment ID: {Id}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Approve a deployment
        /// </summary>
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveDeployment(int id, [FromBody] ApprovalRequest request)
        {
            try
            {
                var result = await _deploymentService.ApproveDeploymentAsync(id, request.ApprovedBy);
                return Ok(new { success = true, data = result, message = "Deployment approved" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving deployment ID: {Id}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Reject a deployment
        /// </summary>
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectDeployment(int id, [FromBody] ApprovalRequest request)
        {
            try
            {
                var result = await _deploymentService.RejectDeploymentAsync(
                    id, request.ApprovedBy, request.Comments ?? "No reason provided");
                return Ok(new { success = true, data = result, message = "Deployment rejected" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting deployment ID: {Id}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cancel a deployment
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelDeployment(int id, [FromQuery] string cancelledBy)
        {
            try
            {
                var result = await _deploymentService.CancelDeploymentAsync(id, cancelledBy);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Deployment not found" });
                }
                return Ok(new { success = true, message = "Deployment cancelled" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling deployment ID: {Id}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get deployment progress
        /// </summary>
        [HttpGet("{id}/progress")]
        public async Task<IActionResult> GetDeploymentProgress(int id)
        {
            try
            {
                var result = await _deploymentService.GetDeploymentProgressAsync(id);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deployment progress for ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update deployment progress (called by clients)
        /// </summary>
        [HttpPost("{id}/update-progress")]
        public async Task<IActionResult> UpdateDeploymentProgress(
            int id,
            [FromQuery] bool success,
            [FromQuery] string? errorMessage = null)
        {
            try
            {
                await _deploymentService.UpdateDeploymentProgressAsync(id, success, errorMessage);
                return Ok(new { success = true, message = "Progress updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deployment progress for ID: {Id}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get deployments by package version
        /// </summary>
        [HttpGet("package/{packageVersionId}")]
        public async Task<IActionResult> GetDeploymentsByPackageVersion(int packageVersionId)
        {
            try
            {
                var result = await _deploymentService.GetDeploymentsByPackageVersionAsync(packageVersionId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deployments for package version: {PackageVersionId}", packageVersionId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get latest deployment for an application
        /// </summary>
        [HttpGet("application/{applicationId}/latest")]
        public async Task<IActionResult> GetLatestDeployment(int applicationId)
        {
            try
            {
                var result = await _deploymentService.GetLatestDeploymentForApplicationAsync(applicationId);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "No deployments found" });
                }
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest deployment for application: {ApplicationId}", applicationId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}