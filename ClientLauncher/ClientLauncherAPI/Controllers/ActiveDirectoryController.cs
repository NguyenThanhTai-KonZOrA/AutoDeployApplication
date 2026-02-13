using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActiveDirectoryController : ControllerBase
    {
        private readonly IActiveDirectoryService _adService;
        private readonly IDeploymentService _deploymentService;
        private readonly ILogger<ActiveDirectoryController> _logger;

        public ActiveDirectoryController(
            IActiveDirectoryService adService,
            IDeploymentService deploymentService,
            ILogger<ActiveDirectoryController> logger)
        {
            _adService = adService;
            _deploymentService = deploymentService;
            _logger = logger;
        }

        /// <summary>
        /// Get all computers from Active Directory
        /// </summary>
        [HttpGet("computers")]
        public async Task<IActionResult> GetComputers([FromQuery] ADComputerSearchRequest? request = null)
        {
            try
            {
                var result = await _adService.GetAllComputersAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting computers from Active Directory");
                return StatusCode(500, new { success = false, message = "Failed to retrieve computers from Active Directory" });
            }
        }

        /// <summary>
        /// Get a specific computer by name
        /// </summary>
        [HttpGet("computers/{computerName}")]
        public async Task<IActionResult> GetComputer(string computerName)
        {
            try
            {
                var result = await _adService.GetComputerByNameAsync(computerName);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Computer not found in Active Directory" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting computer {ComputerName} from Active Directory", computerName);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all Organizational Units
        /// </summary>
        [HttpGet("organizational-units")]
        public async Task<IActionResult> GetOrganizationalUnits()
        {
            try
            {
                var result = await _adService.GetOrganizationalUnitsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organizational units from Active Directory");
                return StatusCode(500, new { success = false, message = "Failed to retrieve organizational units" });
            }
        }

        /// <summary>
        /// Get computers in a specific OU
        /// </summary>
        [HttpGet("organizational-units/{ouPath}/computers")]
        public async Task<IActionResult> GetComputersInOU(string ouPath)
        {
            try
            {
                var decodedPath = System.Net.WebUtility.UrlDecode(ouPath);
                var result = await _adService.GetComputersInOUAsync(decodedPath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting computers from OU: {OUPath}", ouPath);
                return StatusCode(500, new { success = false, message = "Failed to retrieve computers from OU" });
            }
        }

        /// <summary>
        /// Search computers by pattern
        /// </summary>
        [HttpGet("computers/search/{searchPattern}")]
        public async Task<IActionResult> SearchComputers(string searchPattern)
        {
            try
            {
                var result = await _adService.SearchComputersAsync(searchPattern);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching computers with pattern: {Pattern}", searchPattern);
                return StatusCode(500, new { success = false, message = "Failed to search computers" });
            }
        }

        /// <summary>
        /// Check if a computer is online
        /// </summary>
        [HttpGet("computers/{computerName}/online")]
        public async Task<IActionResult> CheckComputerOnline(string computerName)
        {
            try
            {
                var isOnline = await _adService.IsComputerOnlineAsync(computerName);
                return Ok(new { computerName, isOnline, checkedAt = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking online status for {ComputerName}", computerName);
                return StatusCode(500, new { success = false, message = "Failed to check computer status" });
            }
        }

        /// <summary>
        /// Deploy to multiple computers from Active Directory
        /// </summary>
        [HttpPost("bulk-deploy")]
        public async Task<IActionResult> BulkDeployToADComputers([FromBody] ADBulkDeploymentRequest request)
        {
            try
            {
                var deploymentResults = new List<object>();
                var computers = new List<ADComputerResponse>();

                if (request.TargetComputerNames != null && request.TargetComputerNames.Any())
                {
                    foreach (var computerName in request.TargetComputerNames)
                    {
                        var computer = await _adService.GetComputerByNameAsync(computerName);
                        if (computer != null)
                        {
                            computers.Add(computer);
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(request.OrganizationalUnit))
                {
                    var adResult = await _adService.GetComputersInOUAsync(request.OrganizationalUnit);
                    computers.AddRange(adResult.Computers);
                }
                else
                {
                    return BadRequest(new { success = false, message = "Either TargetComputerNames or OrganizationalUnit must be specified" });
                }

                if (request.EnabledComputersOnly)
                {
                    computers = computers.Where(c => c.Enabled).ToList();
                }

                if (request.OnlineComputersOnly)
                {
                    var onlineComputers = new List<ADComputerResponse>();
                    foreach (var computer in computers)
                    {
                        if (await _adService.IsComputerOnlineAsync(computer.DnsHostName ?? computer.Name))
                        {
                            onlineComputers.Add(computer);
                        }
                    }
                    computers = onlineComputers;
                }

                foreach (var computer in computers)
                {
                    try
                    {
                        var deploymentRequest = new DeploymentCreateRequest
                        {
                            PackageVersionId = request.PackageVersionId,
                            Environment = request.Environment,
                            DeploymentType = request.DeploymentType,
                            IsGlobalDeployment = false,
                            TargetMachines = new List<string> { computer.DnsHostName ?? computer.Name },
                            RequiresApproval = request.RequiresApproval,
                            DeployedBy = request.DeployedBy,
                            ScheduledFor = request.ScheduledFor
                        };

                        var deployment = await _deploymentService.CreateDeploymentAsync(deploymentRequest);
                        
                        deploymentResults.Add(new
                        {
                            computerName = computer.Name,
                            dnsHostName = computer.DnsHostName,
                            deploymentId = deployment.Id,
                            success = true
                        });

                        _logger.LogInformation("Created deployment {DeploymentId} for computer {ComputerName}", 
                            deployment.Id, computer.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create deployment for computer {ComputerName}", computer.Name);
                        
                        deploymentResults.Add(new
                        {
                            computerName = computer.Name,
                            dnsHostName = computer.DnsHostName,
                            deploymentId = (int?)null,
                            success = false,
                            error = ex.Message
                        });
                    }
                }

                return Ok(new
                {
                    success = true,
                    totalComputers = computers.Count,
                    deploymentsCreated = deploymentResults.Count(r => ((dynamic)r).success),
                    deploymentsFailed = deploymentResults.Count(r => !((dynamic)r).success),
                    results = deploymentResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk deployment from Active Directory");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Sync computers from AD to database (preview what would be synced)
        /// </summary>
        [HttpPost("sync/preview")]
        public async Task<IActionResult> PreviewADSync([FromBody] ADSyncRequest request)
        {
            try
            {
                var searchRequest = new ADComputerSearchRequest
                {
                    OrganizationalUnit = request.OrganizationalUnit,
                    EnabledOnly = request.EnabledOnly
                };

                var computers = await _adService.GetAllComputersAsync(searchRequest);

                return Ok(new
                {
                    success = true,
                    totalComputers = computers.TotalCount,
                    enabledComputers = computers.EnabledCount,
                    computers = computers.Computers.Select(c => new
                    {
                        c.Name,
                        c.DnsHostName,
                        c.OperatingSystem,
                        c.Enabled,
                        c.LastLogon
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing AD sync");
                return StatusCode(500, new { success = false, message = "Failed to preview sync" });
            }
        }
    }
}
