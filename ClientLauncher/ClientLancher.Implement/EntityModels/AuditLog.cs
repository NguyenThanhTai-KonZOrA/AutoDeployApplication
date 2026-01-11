using ClientLancher.Common.BaseEntity;
using System.ComponentModel.DataAnnotations;

namespace ClientLancher.Implement.EntityModels
{
    public class AuditLog : BaseEntity
    {
        public int ID { get; set; }

        /// <summary>
        /// User who performed the action (username, employee code, etc.)
        /// </summary>
        [Required]
        [StringLength(200)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Action performed (e.g., "CreatePatron", "UpdatePatron", "DeletePatron")
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Entity type affected (e.g., "Patron", "Employee", "Membership")
        /// </summary>
        [StringLength(100)]
        public string? EntityType { get; set; }

        /// <summary>
        /// ID of the affected entity
        /// </summary>
        public int? EntityId { get; set; }

        /// <summary>
        /// HTTP Method (GET, POST, PUT, DELETE)
        /// </summary>
        [StringLength(10)]
        public string? HttpMethod { get; set; }

        /// <summary>
        /// Request path
        /// </summary>
        [StringLength(500)]
        public string? RequestPath { get; set; }

        /// <summary>
        /// IP Address of the user
        /// </summary>
        [StringLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User Agent (browser info)
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Success or Failure
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// HTTP Status Code (200, 400, 500, etc.)
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional details (JSON format recommended)
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Duration of the action in milliseconds
        /// </summary>
        public long? DurationMs { get; set; }
    }
}
