using ClientLancher.Common.Constants;
using ClientLancher.Common.JwtAuthen;
using ClientLancher.Common.SystemConfiguration;
using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;
using ClientLauncherAPI.WindowHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        #region Constructor
        private readonly ISystemConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmployeeService _employeeService;
        public AuthController(
            ISystemConfiguration configuration,
            ILogger<AuthController> logger,
            IRefreshTokenService refreshTokenService,
            IAuditLogService auditLogService,
            IEmployeeService employeeService)
        {
            _configuration = configuration;
            _logger = logger;
            _refreshTokenService = refreshTokenService;
            _auditLogService = auditLogService;
            _employeeService = employeeService;
        }
        #endregion

        #region Main APIs

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            try
            {
                _logger.LogInformation("Login attempt for user {Username}", loginRequest.Username);
                var result = WindowsAuthHelper.WindowsAccount(loginRequest.Username, loginRequest.Password);
                string adminUserName = _configuration.GetValue("AdminAccount:UserName") ?? "admin";
                string adminPassword = _configuration.GetValue("AdminAccount:Password") ?? "123456";
                if (loginRequest.Username == adminUserName && loginRequest.Password == adminPassword)
                {
                    result = 1;
                }

                if (result != 1)
                {
                    _logger.LogError("Invalid credentials.");
                    throw new Exception("The user name or password is incorrect.");
                }

                var employee = await _employeeService.GetOrCreateEmployeeFromWindowsAccountAsync(loginRequest.Username);
                _logger.LogInformation("✅ Employee authenticated: {EmployeeCode} (ID: {EmployeeId})", employee.EmployeeCode, employee.Id);

                // Resolve role
                var adminsConfig = "admin;superuser;";
                var adminUsers = adminsConfig.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var role = adminUsers.Contains(loginRequest.Username, StringComparer.OrdinalIgnoreCase)
                    ? CommonConstants.AdminRole
                    : CommonConstants.UserRole;

                // Resolve role
                string[] rolesName = Array.Empty<string>();
                rolesName = new string[] { CommonConstants.AdminRole };
                var tokenResponse = GenerateTokens(loginRequest.Username, rolesName, employee);

                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = loginRequest.Username,
                    Action = "Login",
                    EntityType = "Authentication",
                    EntityId = employee.Id,
                    HttpMethod = "POST",
                    RequestPath = "/api/auth/login",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IsSuccess = true,
                    StatusCode = 200,
                    ErrorMessage = null,
                    Details = $"User {loginRequest.Username} logged in successfully.",
                    DurationMs = 0 // You can measure actual duration if needed
                });

                _logger.LogInformation("✅ Token generated for {Username} with server_start: {ServerStart}",
                    loginRequest.Username,
                    TokenValidationService.ServerStartTime);

                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = loginRequest.Username,
                    Action = "Login",
                    EntityType = "Authentication",
                    EntityId = null,
                    HttpMethod = "POST",
                    RequestPath = "/api/auth/login",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IsSuccess = false,
                    StatusCode = 401,
                    ErrorMessage = ex.Message,
                    Details = $"Failed login attempt for user {loginRequest.Username}.",
                    DurationMs = 0 // You can measure actual duration if needed
                });

                _logger.LogError(ex, "Login error for user {Username}", loginRequest.Username);
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var username = User.Identity?.Name;

                // If username is null, try to extract it from the JWT token in the Authorization header
                if (string.IsNullOrEmpty(username))
                {
                    var authHeader = Request.Headers["Authorization"].ToString();
                    if (authHeader.StartsWith("Bearer "))
                    {
                        var token = authHeader.Substring("Bearer ".Length).Trim();
                        var handler = new JwtSecurityTokenHandler();
                        var jwtToken = handler.ReadJwtToken(token);
                        username = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                    }
                }

                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (!_refreshTokenService.ValidateRefreshToken(username, request.RefreshToken))
                {
                    _logger.LogWarning("Invalid refresh token for user {Username}", username);
                    return Unauthorized(new { message = "Invalid refresh token" });
                }

                var employee = await _employeeService.GetOrCreateEmployeeFromWindowsAccountAsync(username);

                // Resolve role
                string[] rolesName = Array.Empty<string>();
                rolesName = new string[] { CommonConstants.AdminRole };
                var tokenResponse = GenerateTokens(username, rolesName, employee);

                _logger.LogInformation("✅ Token refreshed for {Username}", username);

                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token error");
                return Unauthorized(new { message = "Token refresh failed" });
            }
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized();
                }

                _refreshTokenService.RevokeRefreshToken(username);
                _logger.LogInformation("Token revoked for user {Username}", username);

                // Log the revoke action
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = username,
                    Action = "RevokeToken",
                    EntityType = "Authentication",
                    EntityId = null,
                    HttpMethod = "POST",
                    RequestPath = "/api/auth/revoke-token",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IsSuccess = true,
                    StatusCode = 200,
                    ErrorMessage = null,
                    Details = $"User {username} revoked their token successfully.",
                    DurationMs = 0 // You can measure actual duration if needed
                });

                return Ok(new { message = "Token revoked successfully" });
            }
            catch (Exception ex)
            {
                // Log the error
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = User.Identity?.Name ?? "Unknown",
                    Action = "RevokeToken",
                    EntityType = "Authentication",
                    EntityId = null,
                    HttpMethod = "POST",
                    RequestPath = "/api/auth/revoke-token",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IsSuccess = false,
                    StatusCode = 500,
                    ErrorMessage = ex.Message,
                    Details = $"Failed to revoke token: {ex.Message}",
                    DurationMs = 0 // You can measure actual duration if needed
                });

                _logger.LogError(ex, "Revoke token error");
                return BadRequest(new { message = "Failed to revoke token" });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var employeeId = User.FindFirstValue("EmployeeId");
            var employeeCode = User.FindFirstValue("EmployeeCode");

            return Ok(new
            {
                userName = User.Identity?.Name,
                role = User.FindFirstValue(ClaimTypes.Role),
                employeeId = employeeId != null ? int.Parse(employeeId) : (int?)null,
                employeeCode = employeeCode
            });
        }

        [HttpGet("server-info")]
        [AllowAnonymous]
        public IActionResult GetServerInfo()
        {
            return Ok(new
            {
                serverStartTime = TokenValidationService.ServerStartTime,
                currentTime = DateTime.UtcNow.ToString("o")
            });
        }
        #endregion

        #region Private Methods
        private LoginResponse GenerateTokens(string username, string[] roles, Employee employee)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, string.Join(",", roles)),
                new Claim("EmployeeId", employee.Id.ToString()),
                new Claim("EmployeeCode", employee.EmployeeCode),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim("server_start", TokenValidationService.ServerStartTime)
            };

            var jwt = _configuration.GetSection<JwtOptions>("Jwt") ?? new JwtOptions();
            if (string.IsNullOrWhiteSpace(jwt.Key))
            {
                _logger.LogError("JWT key is missing in configuration.");
                throw new Exception("JWT key missing.");
            }

            var keyBytes = Encoding.UTF8.GetBytes(jwt.Key);
            var signingKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(jwt.ExpireMinutes > 0 ? jwt.ExpireMinutes : 30);

            var token = new JwtSecurityToken(
                issuer: jwt.Issuer,
                audience: jwt.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Generate refresh token (expires in 7 days)
            var refreshToken = _refreshTokenService.GenerateRefreshToken();
            var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);
            _refreshTokenService.StoreRefreshToken(username, refreshToken, refreshTokenExpiration);

            return new LoginResponse
            {
                UserName = username,
                Token = tokenString,
                RefreshToken = refreshToken,
                Role = roles,
                EmployeeId = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                TokenExpiration = expires
            };
        }
        #endregion
    }
}
