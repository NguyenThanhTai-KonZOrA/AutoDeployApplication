using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.UnitOfWork;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClientLauncher.Implement.Services
{
    public class DeploymentService : IDeploymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeploymentService> _logger;

        public DeploymentService(IUnitOfWork unitOfWork, ILogger<DeploymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DeploymentResponse> CreateDeploymentAsync(DeploymentCreateRequest request)
        {
            try
            {
                _logger.LogInformation("Creating deployment for Package Version ID: {PackageVersionId}",
                    request.PackageVersionId);

                // Validate package version exists
                var packageVersion = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(request.PackageVersionId);
                if (packageVersion == null)
                {
                    throw new Exception($"Package version with ID {request.PackageVersionId} not found");
                }

                // Calculate total targets
                int totalTargets = 0;
                if (request.IsGlobalDeployment)
                {
                    // TODO: Get total number of registered clients from database
                    totalTargets = 0; // Will be updated when clients check in
                }
                else
                {
                    totalTargets = (request.TargetMachines?.Count ?? 0) + (request.TargetUsers?.Count ?? 0);
                }

                var deployment = new DeploymentHistory
                {
                    PackageVersionId = request.PackageVersionId,
                    Environment = request.Environment,
                    DeploymentType = request.DeploymentType,
                    Status = request.RequiresApproval ? "Pending Approval" : "Pending",
                    IsGlobalDeployment = request.IsGlobalDeployment,
                    TargetMachines = request.TargetMachines != null ? JsonSerializer.Serialize(request.TargetMachines) : null,
                    TargetUsers = request.TargetUsers != null ? JsonSerializer.Serialize(request.TargetUsers) : null,
                    TotalTargets = totalTargets,
                    SuccessCount = 0,
                    FailedCount = 0,
                    PendingCount = totalTargets,
                    DeployedBy = request.DeployedBy,
                    DeployedAt = DateTime.UtcNow,
                    RequiresApproval = request.RequiresApproval
                };

                await _unitOfWork.DeploymentHistories.AddAsync(deployment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deployment created successfully: ID {Id}", deployment.Id);

                return MapToResponse(deployment, packageVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deployment");
                throw;
            }
        }

        public async Task<DeploymentResponse> UpdateDeploymentStatusAsync(int id, string status, string? errorMessage = null)
        {
            try
            {
                var deployment = await _unitOfWork.DeploymentHistories.GetByIdAsync(id);
                if (deployment == null)
                {
                    throw new Exception($"Deployment with ID {id} not found");
                }

                deployment.Status = status;
                deployment.ErrorMessage = errorMessage;

                if (status == "Success" || status == "Failed" || status == "Cancelled")
                {
                    deployment.CompletedAt = DateTime.UtcNow;
                }

                _unitOfWork.DeploymentHistories.Update(deployment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deployment {Id} status updated to: {Status}", id, status);

                var packageVersion = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(deployment.PackageVersionId);
                return MapToResponse(deployment, packageVersion!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deployment status for ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> CancelDeploymentAsync(int id, string cancelledBy)
        {
            try
            {
                var deployment = await _unitOfWork.DeploymentHistories.GetByIdAsync(id);
                if (deployment == null)
                {
                    return false;
                }

                if (deployment.Status == "Success" || deployment.Status == "Failed")
                {
                    throw new Exception("Cannot cancel a completed deployment");
                }

                deployment.Status = "Cancelled";
                deployment.CompletedAt = DateTime.UtcNow;
                deployment.ErrorMessage = $"Cancelled by {cancelledBy}";

                _unitOfWork.DeploymentHistories.Update(deployment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deployment {Id} cancelled by {User}", id, cancelledBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling deployment ID: {Id}", id);
                throw;
            }
        }

        public async Task<DeploymentResponse?> GetDeploymentByIdAsync(int id)
        {
            var deployment = await _unitOfWork.DeploymentHistories.GetByIdAsync(id);
            if (deployment == null)
                return null;

            var packageVersion = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(deployment.PackageVersionId);
            return MapToResponse(deployment, packageVersion!);
        }

        public async Task<IEnumerable<DeploymentResponse>> GetAllDeploymentsAsync()
        {
            var deployments = await _unitOfWork.DeploymentHistories.GetRecentDeploymentsAsync(100);
            return await MapToResponseListAsync(deployments);
        }

        public async Task<IEnumerable<DeploymentResponse>> GetDeploymentsByEnvironmentAsync(string environment)
        {
            var deployments = await _unitOfWork.DeploymentHistories.GetByEnvironmentAsync(environment);
            return await MapToResponseListAsync(deployments);
        }

        public async Task<IEnumerable<DeploymentResponse>> GetPendingDeploymentsAsync()
        {
            var deployments = await _unitOfWork.DeploymentHistories.GetPendingDeploymentsAsync();
            return await MapToResponseListAsync(deployments);
        }

        public async Task<DeploymentResponse> ApproveDeploymentAsync(int id, string approvedBy)
        {
            try
            {
                var deployment = await _unitOfWork.DeploymentHistories.GetByIdAsync(id);
                if (deployment == null)
                {
                    throw new Exception($"Deployment with ID {id} not found");
                }

                if (!deployment.RequiresApproval)
                {
                    throw new Exception("This deployment does not require approval");
                }

                if (deployment.ApprovedAt != null)
                {
                    throw new Exception("Deployment is already approved");
                }

                deployment.ApprovedBy = approvedBy;
                deployment.ApprovedAt = DateTime.UtcNow;
                deployment.Status = "Approved"; // Ready to deploy

                _unitOfWork.DeploymentHistories.Update(deployment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deployment {Id} approved by {User}", id, approvedBy);

                var packageVersion = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(deployment.PackageVersionId);
                return MapToResponse(deployment, packageVersion!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving deployment ID: {Id}", id);
                throw;
            }
        }

        public async Task<DeploymentResponse> RejectDeploymentAsync(int id, string rejectedBy, string reason)
        {
            try
            {
                var deployment = await _unitOfWork.DeploymentHistories.GetByIdAsync(id);
                if (deployment == null)
                {
                    throw new Exception($"Deployment with ID {id} not found");
                }

                deployment.Status = "Rejected";
                deployment.CompletedAt = DateTime.UtcNow;
                deployment.ErrorMessage = $"Rejected by {rejectedBy}. Reason: {reason}";

                _unitOfWork.DeploymentHistories.Update(deployment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deployment {Id} rejected by {User}", id, rejectedBy);

                var packageVersion = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(deployment.PackageVersionId);
                return MapToResponse(deployment, packageVersion!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting deployment ID: {Id}", id);
                throw;
            }
        }

        public async Task UpdateDeploymentProgressAsync(int id, bool success, string? errorMessage = null)
        {
            try
            {
                var deployment = await _unitOfWork.DeploymentHistories.GetByIdAsync(id);
                if (deployment == null)
                {
                    throw new Exception($"Deployment with ID {id} not found");
                }

                if (success)
                {
                    deployment.SuccessCount++;
                }
                else
                {
                    deployment.FailedCount++;
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        deployment.ErrorMessage = (deployment.ErrorMessage ?? "") + $"\n{errorMessage}";
                    }
                }

                deployment.PendingCount = deployment.TotalTargets - deployment.SuccessCount - deployment.FailedCount;

                // Auto-complete if all targets processed
                if (deployment.PendingCount <= 0)
                {
                    deployment.Status = deployment.FailedCount > 0 ? "Completed with Errors" : "Success";
                    deployment.CompletedAt = DateTime.UtcNow;
                }

                _unitOfWork.DeploymentHistories.Update(deployment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deployment {Id} progress updated: Success={Success}, Failed={Failed}, Pending={Pending}",
                    id, deployment.SuccessCount, deployment.FailedCount, deployment.PendingCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deployment progress for ID: {Id}", id);
                throw;
            }
        }

        public async Task<DeploymentProgressResponse> GetDeploymentProgressAsync(int id)
        {
            var deployment = await _unitOfWork.DeploymentHistories.GetByIdAsync(id);
            if (deployment == null)
            {
                throw new Exception($"Deployment with ID {id} not found");
            }

            var packageVersion = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(deployment.PackageVersionId);

            var elapsedTime = deployment.CompletedAt.HasValue
                ? deployment.CompletedAt.Value - deployment.DeployedAt
                : DateTime.UtcNow - deployment.DeployedAt;

            // Estimate completion time based on current progress
            DateTime? estimatedCompletion = null;
            if (deployment.SuccessCount + deployment.FailedCount > 0 && deployment.PendingCount > 0)
            {
                var avgTimePerTarget = elapsedTime.TotalSeconds / (deployment.SuccessCount + deployment.FailedCount);
                var remainingSeconds = avgTimePerTarget * deployment.PendingCount;
                estimatedCompletion = DateTime.UtcNow.AddSeconds(remainingSeconds);
            }

            return new DeploymentProgressResponse
            {
                DeploymentId = deployment.Id,
                ApplicationName = packageVersion!.Application.Name,
                Version = packageVersion.Version,
                Status = deployment.Status,
                TotalTargets = deployment.TotalTargets,
                SuccessCount = deployment.SuccessCount,
                FailedCount = deployment.FailedCount,
                PendingCount = deployment.PendingCount,
                StartedAt = deployment.DeployedAt,
                EstimatedCompletion = estimatedCompletion,
                ElapsedTime = elapsedTime,
                TargetStatuses = new List<DeploymentTargetStatus>() // TODO: Track individual targets
            };
        }

        public async Task<DeploymentResponse> StartDeploymentAsync(int id)
        {
            try
            {
                var deployment = await _unitOfWork.DeploymentHistories.GetByIdAsync(id);
                if (deployment == null)
                {
                    throw new Exception($"Deployment with ID {id} not found");
                }

                if (deployment.RequiresApproval && deployment.ApprovedAt == null)
                {
                    throw new Exception("Deployment requires approval before starting");
                }

                deployment.Status = "InProgress";
                _unitOfWork.DeploymentHistories.Update(deployment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deployment {Id} started", id);

                var packageVersion = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(deployment.PackageVersionId);
                return MapToResponse(deployment, packageVersion!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting deployment ID: {Id}", id);
                throw;
            }
        }

        public async Task CompleteDeploymentAsync(int id)
        {
            try
            {
                var deployment = await _unitOfWork.DeploymentHistories.GetByIdAsync(id);
                if (deployment == null)
                {
                    throw new Exception($"Deployment with ID {id} not found");
                }

                deployment.Status = deployment.FailedCount > 0 ? "Completed with Errors" : "Success";
                deployment.CompletedAt = DateTime.UtcNow;

                _unitOfWork.DeploymentHistories.Update(deployment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deployment {Id} completed with status: {Status}", id, deployment.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing deployment ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<DeploymentResponse>> GetDeploymentsByPackageVersionAsync(int packageVersionId)
        {
            var deployments = await _unitOfWork.DeploymentHistories.GetByPackageVersionIdAsync(packageVersionId);
            return await MapToResponseListAsync(deployments);
        }

        public async Task<IEnumerable<DeploymentResponse>> GetDeploymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var allDeployments = await _unitOfWork.DeploymentHistories.GetAllAsync();
            var filtered = allDeployments.Where(d => d.DeployedAt >= startDate && d.DeployedAt <= endDate);
            return await MapToResponseListAsync(filtered);
        }

        public async Task<DeploymentResponse?> GetLatestDeploymentForApplicationAsync(int applicationId)
        {
            var versions = await _unitOfWork.PackageVersions.GetByApplicationIdAsync(applicationId);
            if (!versions.Any())
                return null;

            DeploymentHistory? latestDeployment = null;
            foreach (var version in versions)
            {
                var deployment = await _unitOfWork.DeploymentHistories.GetLatestDeploymentAsync(version.Id);
                if (deployment != null && (latestDeployment == null || deployment.DeployedAt > latestDeployment.DeployedAt))
                {
                    latestDeployment = deployment;
                }
            }

            if (latestDeployment == null)
                return null;

            var packageVersion = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(latestDeployment.PackageVersionId);
            return MapToResponse(latestDeployment, packageVersion!);
        }

        private DeploymentResponse MapToResponse(DeploymentHistory deployment, PackageVersion packageVersion)
        {
            return new DeploymentResponse
            {
                Id = deployment.Id,
                PackageVersionId = deployment.PackageVersionId,
                ApplicationName = packageVersion.Application.Name,
                Version = packageVersion.Version,
                Environment = deployment.Environment,
                DeploymentType = deployment.DeploymentType,
                Status = deployment.Status,
                IsGlobalDeployment = deployment.IsGlobalDeployment,
                TotalTargets = deployment.TotalTargets,
                SuccessCount = deployment.SuccessCount,
                FailedCount = deployment.FailedCount,
                PendingCount = deployment.PendingCount,
                DeployedBy = deployment.DeployedBy,
                DeployedAt = deployment.DeployedAt,
                CompletedAt = deployment.CompletedAt,
                ErrorMessage = deployment.ErrorMessage,
                RequiresApproval = deployment.RequiresApproval,
                ApprovedBy = deployment.ApprovedBy,
                ApprovedAt = deployment.ApprovedAt
            };
        }

        private async Task<IEnumerable<DeploymentResponse>> MapToResponseListAsync(IEnumerable<DeploymentHistory> deployments)
        {
            var responses = new List<DeploymentResponse>();
            foreach (var deployment in deployments)
            {
                var packageVersion = await _unitOfWork.PackageVersions.GetByIdWithDetailsAsync(deployment.PackageVersionId);
                if (packageVersion != null)
                {
                    responses.Add(MapToResponse(deployment, packageVersion));
                }
            }
            return responses;
        }
    }
}