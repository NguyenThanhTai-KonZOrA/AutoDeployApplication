using ClientLauncher.Common.Constants;
using System.ComponentModel.DataAnnotations;

namespace ClientLauncher.Common.BaseEntity
{
    public abstract class BaseEntity
    {
        public bool IsActive { get; set; } = true;
        public bool IsDelete { get; set; } = false;
        [StringLength(100)]
        public string CreatedBy { get; set; } = CommonConstants.SystemUser;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(100)]
        public string UpdatedBy { get; set; } = CommonConstants.SystemUser;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}