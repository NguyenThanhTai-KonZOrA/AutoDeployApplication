namespace ClientLauncher.Implement.ViewModels.Request
{
    public class ADComputerSearchRequest
    {
        public string? OrganizationalUnit { get; set; }
        public string? SearchPattern { get; set; }
        public bool EnabledOnly { get; set; } = true;
        public bool CheckOnlineStatus { get; set; } = false;
        public string? OperatingSystemFilter { get; set; }
    }

    public class ADBulkDeploymentRequest
    {
        public int PackageVersionId { get; set; }
        public string? OrganizationalUnit { get; set; }
        public List<string>? TargetComputerNames { get; set; }
        public string Environment { get; set; } = "Production";
        public string DeploymentType { get; set; } = "Release";
        public bool RequiresApproval { get; set; } = false;
        public string DeployedBy { get; set; } = string.Empty;
        public DateTime? ScheduledFor { get; set; }
        public bool EnabledComputersOnly { get; set; } = true;
        public bool OnlineComputersOnly { get; set; } = false;
    }

    public class ADSyncRequest
    {
        public string? OrganizationalUnit { get; set; }
        public bool AutoRegisterNewMachines { get; set; } = true;
        public bool UpdateExistingMachines { get; set; } = true;
        public bool EnabledOnly { get; set; } = true;
    }
}
