using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLancher.Implement.ViewModels
{
    /// <summary>
    /// Result of token validation
    /// </summary>
    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public TokenRejectionReason? RejectionReason { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }

        public static TokenValidationResult Success(string username, string role)
        {
            return new TokenValidationResult
            {
                IsValid = true,
                Username = username,
                Role = role
            };
        }

        public static TokenValidationResult Failure(string errorMessage, TokenRejectionReason reason)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                RejectionReason = reason
            };
        }
    }

    public enum TokenRejectionReason
    {
        Missing,
        InvalidFormat,
        InvalidSignature,
        Expired,
        ServerRestarted,
        Unknown
    }
}
