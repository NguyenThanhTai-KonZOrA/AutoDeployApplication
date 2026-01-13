using ClientLauncher.Common.JwtAuthen;
using ClientLauncher.Common.SystemConfiguration;
using ClientLauncher.Implement.ViewModels;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TokenValidationResult = ClientLauncher.Implement.ViewModels.TokenValidationResult;

namespace ClientLauncherAPI.WindowHelpers
{
    public class TokenValidationService
    {
        private readonly ISystemConfiguration _configuration;
        private readonly ILogger<TokenValidationService> _logger;

        /// <summary>
        /// ✅ Server start time - static so it persists across requests
        /// This is used to invalidate all tokens issued before server restart
        /// </summary>
        public static readonly string ServerStartTime = DateTime.UtcNow.ToString("o");

        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly TokenValidationParameters _validationParameters;

        public TokenValidationService(
            ISystemConfiguration configuration,
            ILogger<TokenValidationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _tokenHandler = new JwtSecurityTokenHandler();

            var jwt = _configuration.GetSection<JwtOptions>("Jwt") ?? new JwtOptions();

            _validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.Key ?? throw new InvalidOperationException("JWT Key not configured"))
                ),
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };

            _logger.LogInformation("🔐 TokenValidationService initialized. Server Start Time: {ServerStartTime}", ServerStartTime);
        }

        /// <summary>
        /// Get current server start time (for debugging)
        /// </summary>
        public string GetServerStartTime() => ServerStartTime;

        /// <summary>
        /// Validate JWT token from Authorization header
        /// </summary>
        public TokenValidationResult ValidateToken(string? authorizationHeader)
        {
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                _logger.LogWarning("⚠️ Token validation failed: Missing Authorization header");
                return TokenValidationResult.Failure(
                    "Authorization header is missing",
                    TokenRejectionReason.Missing
                );
            }

            var parts = authorizationHeader.Split(' ');
            if (parts.Length != 2 || !parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("⚠️ Token validation failed: Invalid Authorization header format");
                return TokenValidationResult.Failure(
                    "Invalid Authorization header format. Expected: 'Bearer {token}'",
                    TokenRejectionReason.InvalidFormat
                );
            }

            var token = parts[1];
            return ValidateTokenString(token);
        }

        /// <summary>
        /// Validate JWT token string
        /// </summary>
        public TokenValidationResult ValidateTokenString(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return TokenValidationResult.Failure(
                    "Token is empty",
                    TokenRejectionReason.Missing
                );
            }

            try
            {
                // Step 1: Read token without validation
                JwtSecurityToken? jwtToken;
                try
                {
                    jwtToken = _tokenHandler.ReadJwtToken(token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ Token validation failed: Invalid token format - {Error}", ex.Message);
                    return TokenValidationResult.Failure(
                        "Invalid token format",
                        TokenRejectionReason.InvalidFormat
                    );
                }

                // NEW: Check token expiration BEFORE full validation
                var exp = jwtToken.ValidTo;
                if (exp <= DateTime.UtcNow)
                {
                    _logger.LogWarning("⚠️ Token validation failed: Token expired at {ExpTime}, current time: {Now}",
                        exp, DateTime.UtcNow);
                    return TokenValidationResult.Failure(
                        "Token has expired. Please login again.",
                        TokenRejectionReason.Expired
                    );
                }

                // Step 2: Check server_start claim
                var tokenServerStart = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "server_start")?.Value;

                if (string.IsNullOrEmpty(tokenServerStart))
                {
                    _logger.LogWarning("⚠️ Token validation failed: Missing server_start claim (old token format)");
                    return TokenValidationResult.Failure(
                        "Token is outdated. Please re-login.",
                        TokenRejectionReason.ServerRestarted
                    );
                }

                if (tokenServerStart != ServerStartTime)
                {
                    _logger.LogWarning(
                        "❌ Token rejected: Server was restarted\n" +
                        "   Token server_start: {TokenStart}\n" +
                        "   Current server_start: {CurrentStart}",
                        tokenServerStart,
                        ServerStartTime
                    );

                    return TokenValidationResult.Failure(
                        "Your session is no longer valid. Please login again.",
                        TokenRejectionReason.ServerRestarted
                    );
                }

                // Step 3: Validate token signature and expiration (double-check)
                ClaimsPrincipal? principal;
                try
                {
                    principal = _tokenHandler.ValidateToken(token, _validationParameters, out _);
                }
                catch (SecurityTokenExpiredException)
                {
                    _logger.LogWarning("⚠️ Token validation failed: Token has expired");
                    return TokenValidationResult.Failure(
                        "Token has expired. Please login again.",
                        TokenRejectionReason.Expired
                    );
                }
                catch (SecurityTokenInvalidSignatureException)
                {
                    _logger.LogWarning("⚠️ Token validation failed: Invalid signature");
                    return TokenValidationResult.Failure(
                        "Invalid token signature",
                        TokenRejectionReason.InvalidSignature
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Token validation failed: {Error}", ex.Message);
                    return TokenValidationResult.Failure(
                        $"Token validation failed: {ex.Message}",
                        TokenRejectionReason.Unknown
                    );
                }

                // Step 4: Extract claims
                var username = principal.FindFirst(ClaimTypes.Name)?.Value;
                var role = principal.FindFirst(ClaimTypes.Role)?.Value;

                _logger.LogInformation(
                    "✅ Token validated successfully for user: {Username}, role: {Role}",
                    username,
                    role
                );

                return TokenValidationResult.Success(
                    username ?? "unknown",
                    role ?? "user"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error during token validation");
                return TokenValidationResult.Failure(
                    "An error occurred while validating token",
                    TokenRejectionReason.Unknown
                );
            }
        }

        /// <summary>
        /// Validate token from HttpContext request
        /// </summary>
        public TokenValidationResult ValidateRequest(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            return ValidateToken(authHeader);
        }
    }
}
