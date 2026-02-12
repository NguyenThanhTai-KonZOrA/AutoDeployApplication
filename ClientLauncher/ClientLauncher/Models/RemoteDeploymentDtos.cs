using System;
using System.Collections.Generic;

namespace ClientLauncher.Models
{
    public class ClientMachineRegisterDto
    {
        public string MachineId { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string? ComputerName { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? DomainName { get; set; }
        public string? IPAddress { get; set; }
        public string? MACAddress { get; set; }
        public string? OSVersion { get; set; }
        public string? OSArchitecture { get; set; }
        public string? CPUInfo { get; set; }
        public long? TotalMemoryMB { get; set; }
        public long? AvailableDiskSpaceGB { get; set; }
        public List<string>? InstalledApplications { get; set; }
        public string? ClientVersion { get; set; }
        public string? Location { get; set; }
    }

    public class ClientMachineHeartbeatDto
    {
        public string MachineId { get; set; } = string.Empty;
        public string Status { get; set; } = "Online";
        public List<string>? InstalledApplications { get; set; }
        public long? AvailableDiskSpaceGB { get; set; }
    }

    public class DeploymentTaskDto
    {
        public int Id { get; set; }
        public int DeploymentHistoryId { get; set; }
        public int TargetMachineId { get; set; }
        public string AppCode { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
        public DateTime? ScheduledFor { get; set; }
    }

    public class DeploymentTaskUpdateDto
    {
        public int TaskId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public string? CurrentStep { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public long? DownloadSizeBytes { get; set; }
    }
}
