namespace ClientLauncher.Implement.ViewModels.Request
{
    public class PublishPackageRequest
    {
        public int PackageVersionId { get; set; }
        public string PublishedBy { get; set; } = string.Empty;
        public bool SetAsLatest { get; set; } = true;
    }
}