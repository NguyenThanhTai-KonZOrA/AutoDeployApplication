namespace ClientLauncher.Implement.ViewModels.Response
{
    public class LatestManifestByAppCodeResponse
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string AppCode { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }
}