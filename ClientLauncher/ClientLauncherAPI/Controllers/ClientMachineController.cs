using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientMachineController : ControllerBase
    {
        private readonly IClientMachineService _clientMachineService;
        private readonly ILogger<ClientMachineController> _logger;

        public ClientMachineController(
            IClientMachineService clientMachineService,
            ILogger<ClientMachineController> logger)
        {
            _clientMachineService = clientMachineService;
            _logger = logger;
        }

        /// <summary>
        /// Register or update a client machine
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterMachine([FromBody] ClientMachineRegisterRequest request)
        {
            try
            {
                var result = await _clientMachineService.RegisterMachineAsync(request);
                return Ok(new { success = true, data = result, message = "Machine registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering machine: {MachineId}", request.MachineId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update heartbeat for a machine
        /// </summary>
        [HttpPost("heartbeat")]
        public async Task<IActionResult> UpdateHeartbeat([FromBody] ClientMachineHeartbeatRequest request)
        {
            try
            {
                var result = await _clientMachineService.UpdateHeartbeatAsync(request);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Machine not found" });
                }
                return Ok(new { success = true, message = "Heartbeat updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating heartbeat: {MachineId}", request.MachineId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get all online machines
        /// </summary>
        [HttpGet("online")]
        public async Task<IActionResult> GetOnlineMachines()
        {
            try
            {
                var result = await _clientMachineService.GetOnlineMachinesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online machines");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all machines
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllMachines()
        {
            try
            {
                var result = await _clientMachineService.GetAllMachinesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all machines");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get machine by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMachineById(int id)
        {
            try
            {
                var result = await _clientMachineService.GetMachineByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Machine not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machine by ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get machine by machine ID
        /// </summary>
        [HttpGet("by-machine-id/{machineId}")]
        public async Task<IActionResult> GetMachineByMachineId(string machineId)
        {
            try
            {
                var result = await _clientMachineService.GetMachineByMachineIdAsync(machineId);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Machine not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machine by MachineId: {MachineId}", machineId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get machines with specific app installed
        /// </summary>
        [HttpGet("with-app/{appCode}")]
        public async Task<IActionResult> GetMachinesWithApp(string appCode)
        {
            try
            {
                var result = await _clientMachineService.GetMachinesWithAppAsync(appCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machines with app: {AppCode}", appCode);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get client machine statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var result = await _clientMachineService.GetStatisticsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}
