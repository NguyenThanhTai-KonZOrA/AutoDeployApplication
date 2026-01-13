using ClientLauncher.Implement.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeController(
            ILogger<EmployeeController> logger,
            IEmployeeRepository employeeRepository)
        {
            _logger = logger;
            _employeeRepository = employeeRepository;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetEmployeeListAsync()
        {
            try
            {
                _logger.LogInformation("[GetEmployeeListAsync]: called");
                var employees = await _employeeRepository.GetActiveEmployeesAsync();
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetEmployeeListAsync]: Error occurred while retrieving employee list");
                return StatusCode(500, "Internal server error while retrieving employees.");
            }
        }
    }
}