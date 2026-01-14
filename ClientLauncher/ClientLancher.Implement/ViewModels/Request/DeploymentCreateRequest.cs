namespace ClientLauncher.Implement.ViewModels.Request
{
    public class DeploymentCreateRequest
    {
        public int PackageVersionId { get; set; }
        public string Environment { get; set; } = "Production";
        public string DeploymentType { get; set; } = "Release";
        public bool IsGlobalDeployment { get; set; } = true;
        public List<string>? TargetMachines { get; set; }
        public List<string>? TargetUsers { get; set; }
        public bool RequiresApproval { get; set; }
        public string DeployedBy { get; set; } = string.Empty;
    }
}