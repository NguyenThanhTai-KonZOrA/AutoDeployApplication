namespace ClientLauncher.Models.Response
{
    public class IsInstalledResponse
    {
        public string AppCode { get; set; } = string.Empty;
        public bool IsInstalled { get; set; }
    }
}