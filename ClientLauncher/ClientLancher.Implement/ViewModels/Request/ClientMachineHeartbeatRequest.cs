namespace ClientLauncher.Implement.ViewModels.Request
{
    public class ClientMachineHeartbeatRequest
    {
        public string MachineId { get; set; } = string.Empty;
        public string Status { get; set; } = "Online"; // Online, Busy
        public List<string>? InstalledApplications { get; set; }
        public long? AvailableDiskSpaceGB { get; set; }
    }
}
