using ClientLauncher.Common.ApiClient;
using ClientLauncher.Common.Constants;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.UnitOfWork;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClientLauncher.Implement.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EmployeeService> _logger;
        private readonly IApiClient _apiClient;
        private readonly IConfiguration _configuration;
        private readonly IRoleService _roleService;
        private readonly IApplicationSettingsService _applicationSettingsService;

        public EmployeeService(
            IEmployeeRepository employeeRepository,
            IUnitOfWork unitOfWork,
            ILogger<EmployeeService> logger,
            IApiClient apiClient,
            IConfiguration configuration,
            IRoleService roleService,
            IApplicationSettingsService applicationSettingsService)
        {
            _employeeRepository = employeeRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _apiClient = apiClient;
            _configuration = configuration;
            _roleService = roleService;
            _applicationSettingsService = applicationSettingsService;
        }

        public async Task<Employee> GetOrCreateEmployeeFromWindowsAccountAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username cannot be empty", nameof(username));
            }

            var employee = await _employeeRepository.GetByEmployeeByCodeOrUserNameAsync(username);
            if (employee != null)
            {
                _logger.LogInformation("Employee found: {EmployeeCode}", employee.EmployeeCode);
                return employee;
            }

            var theGrandEmployee = await GetTheGrandEmployeeByUserNameAsync(username);

            if (!string.IsNullOrEmpty(theGrandEmployee.adUserName) && !string.IsNullOrEmpty(theGrandEmployee.employeeID))
            {
                // Create new employee from Windows account
                _logger.LogInformation("Creating new employee for code: {EmployeeCode}", theGrandEmployee.employeeID);
                employee = new Employee
                {
                    EmployeeCode = theGrandEmployee?.employeeID ?? username,
                    WindowAccount = username,
                    Department = theGrandEmployee?.departmentName,
                    Position = theGrandEmployee?.position,
                    FullName = theGrandEmployee?.fullName ?? username,
                    Email = $"{username}@thegrandhotram.com",
                    CreatedBy = CommonConstants.SystemUser,
                    UpdatedBy = CommonConstants.SystemUser,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsDelete = false
                };
                await _employeeRepository.AddAsync(employee);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("New employee created: {EmployeeCode} (ID: {EmployeeId})",
                    employee.EmployeeCode, employee.Id);
                return employee;
            }
            else
            {
                throw new ArgumentException("Username cannot found", nameof(username));
            }
        }

        public async Task<Employee> GetOrCreateDefaultEmployeeAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username cannot be empty", nameof(username));
            }

            var employee = await _employeeRepository.GetByEmployeeByCodeOrUserNameAsync(username);
            if (employee != null)
            {
                _logger.LogInformation("Employee found: {EmployeeCode}", employee.EmployeeCode);
                return employee;
            }
            else
            {
                // Create new employee from Windows account
                employee = new Employee
                {
                    EmployeeCode = username,
                    WindowAccount = username,
                    Department = "Information Technology",
                    Position = "Default PC",
                    FullName = username,
                    Email = $"{username}@thegrandhotram.com",
                    CreatedBy = CommonConstants.SystemUser,
                    UpdatedBy = CommonConstants.SystemUser,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsDelete = false
                };
                await _employeeRepository.AddAsync(employee);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("New employee created: {EmployeeCode} (ID: {EmployeeId})",
                    employee.EmployeeCode, employee.Id);
                return employee;
            }
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            _logger.LogInformation("Fetching active employees");
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee != null)
            {
                employee.IsDelete = true;
                employee.IsActive = false;
                employee.UpdatedAt = DateTime.UtcNow;
                _employeeRepository.Update(employee);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<TheGrandEmployeeResponse> GetTheGrandEmployeeByUserNameAsync(string userName)
        {
            _logger.LogInformation("Fetching The Grand employee info for username: {UserName}", userName);
            string baseUrl = _configuration.GetValue<string>("TheGrandEmployeeUrl") ?? "http://10.21.10.1:6969/EmployeeApis/getEmployeeInfoByADUser";
            var response = await _apiClient.GetAsync<TheGrandEmployeeBaseResponse>($"{baseUrl}/{userName}");

            if (response != null && response?.result == "Success" && response?.data.Count > 0)
            {
                _logger.LogInformation("Successfully retrieved The Grand employee info for username: {UserName}", userName);
                return response.data.FirstOrDefault() ?? new TheGrandEmployeeResponse();
            }

            _logger.LogWarning("Failed to retrieve The Grand employee info for username: {UserName}", userName);
            return new TheGrandEmployeeResponse();
        }

        public async Task<Employee?> GetEmployeeByCodeAsync(string employeeCode)
        {
            return await _employeeRepository.GetByEmployeeByCodeOrUserNameAsync(employeeCode);
        }

        public async Task<bool> IsUserAdminAsync(string userName)
        {
            bool checkAdminSetting = _applicationSettingsService.GetSettingValue<bool>(CommonConstants.EnableCheckAdministratorKey);

            if (!checkAdminSetting)
            {
                _logger.LogInformation("Admin role check is disabled.");
                return true;
            }

            var employee = await _employeeRepository.GetByEmployeeByCodeOrUserNameAsync(userName);
            if (employee == null)
            {
                _logger.LogWarning("Employee not found for username: {UserName}", userName);
                return false;
            }
            var roleIds = employee.EmployeeRoles?.Select(er => er.RoleId).ToList() ?? new List<int>();
            var activeRoles = await _roleService.GetActiveRolesByIdsAsync(roleIds);

            bool isAdmin = activeRoles.Any(r => r.RoleName.Equals(CommonConstants.AdminRole, StringComparison.OrdinalIgnoreCase));

            _logger.LogInformation("User {UserName} admin status: {IsAdmin}", userName, isAdmin);

            return isAdmin;
        }

        public async Task<List<Employee>> GetActiveEmployeesAsync()
        {
            var employees = await _employeeRepository.GetActiveEmployeesAsync();
            return employees;
        }
    }
}