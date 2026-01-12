using System.Security.Claims;

namespace ClientLauncherAPI.WindowHelpers
{
    public static class EmployeeHelper
    {
        /// <summary>
        /// Get EmployeeId from current authenticated user's JWT token
        /// </summary>
        public static int? GetCurrentEmployeeId(HttpContext httpContext)
        {
            // Add debug logging
            var allClaims = httpContext.User?.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
            Console.WriteLine($"🔍 All claims: {string.Join(", ", allClaims ?? new List<string>())}");

            var employeeIdClaim = httpContext.User?.FindFirstValue("EmployeeId");
            Console.WriteLine($"🔍 EmployeeId claim value: {employeeIdClaim ?? "NULL"}");

            if (string.IsNullOrEmpty(employeeIdClaim))
                return null;

            return int.TryParse(employeeIdClaim, out var employeeId) ? employeeId : null;
        }

        /// <summary>
        /// Get EmployeeCode from current authenticated user's JWT token
        /// </summary>
        public static string? GetCurrentEmployeeCode(HttpContext httpContext)
        {
            return httpContext.User?.FindFirstValue("EmployeeCode");
        }

        /// <summary>
        /// Get username from current authenticated user
        /// </summary>
        public static string? GetCurrentUsername(HttpContext httpContext)
        {
            return httpContext.User?.Identity?.Name;
        }
    }
}
