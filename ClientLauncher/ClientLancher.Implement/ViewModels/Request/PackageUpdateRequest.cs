namespace ClientLancher.Implement.ViewModels.Request
{
    public class PackageUpdateRequest
    {
        public string? ReleaseNotes { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsStable { get; set; }
        public string? MinimumClientVersion { get; set; }
    }
}