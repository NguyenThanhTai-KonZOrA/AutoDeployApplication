namespace ClientLauncher.Implement.ViewModels.Request
{
    public class ClientMachineRegisterRequest
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
}
