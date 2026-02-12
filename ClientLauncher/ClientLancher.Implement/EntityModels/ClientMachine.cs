using ClientLauncher.Common.BaseEntity;

namespace ClientLauncher.Implement.EntityModels
{
    public class ClientMachine : BaseEntity
    {
        public int Id { get; set; }

        // Machine Identification
        public string MachineId { get; set; } = string.Empty; // Unique identifier (GUID or MAC address)
        public string MachineName { get; set; } = string.Empty;
        public string? ComputerName { get; set; } // Computer name from OS
        
        // User Information
        public string UserName { get; set; } = string.Empty; // Current logged in user
        public string? DomainName { get; set; }
        
        // Network Information
        public string? IPAddress { get; set; }
        public string? MACAddress { get; set; }
        
        // System Information
        public string? OSVersion { get; set; }
        public string? OSArchitecture { get; set; } // x64, x86
        public string? CPUInfo { get; set; }
        public long? TotalMemoryMB { get; set; }
        public long? AvailableDiskSpaceGB { get; set; }
        
        // Status
        public string Status { get; set; } = "Offline"; // Online, Offline, Busy
        public DateTime? LastHeartbeat { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        
        // Installed Applications (JSON array of AppCodes)
        public string? InstalledApplications { get; set; }
        
        // Client Version
        public string? ClientVersion { get; set; }
        
        // Additional Info
        public string? Location { get; set; } // Physical location/department
        public string? Notes { get; set; }

        // Navigation Properties
        public ICollection<DeploymentTask> DeploymentTasks { get; set; } = new List<DeploymentTask>();
    }
}
