namespace ClientLauncher.Implement.ViewModels.Response
{
    public class ADComputerResponse
    {
        public string Name { get; set; } = string.Empty;
        public string? DnsHostName { get; set; }
        public string? DistinguishedName { get; set; }
        public string? OperatingSystem { get; set; }
        public string? OperatingSystemVersion { get; set; }
        public bool Enabled { get; set; }
        public DateTime? LastLogon { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsOnline { get; set; }
    }

    public class ADComputerListResponse
    {
        public List<ADComputerResponse> Computers { get; set; } = new();
        public int TotalCount { get; set; }
        public int EnabledCount { get; set; }
        public int OnlineCount { get; set; }
    }

    public class ADOrganizationalUnitResponse
    {
        public string Name { get; set; } = string.Empty;
        public string DistinguishedName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ComputerCount { get; set; }
    }
}
