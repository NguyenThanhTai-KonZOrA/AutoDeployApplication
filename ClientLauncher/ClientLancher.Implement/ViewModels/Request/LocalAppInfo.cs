namespace ClientLancher.Implement.ViewModels.Request
{
    public class LocalAppInfo
    {
        public string AppCode { get; set; } = string.Empty;
        public string BinaryVersion { get; set; } = "0.0.0";
        public string ConfigVersion { get; set; } = "0.0.0";
    }
}