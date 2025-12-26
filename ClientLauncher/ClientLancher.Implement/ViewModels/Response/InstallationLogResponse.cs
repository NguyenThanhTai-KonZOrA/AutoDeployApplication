namespace ClientLancher.Implement.ViewModels.Response
{
    public class InstallationLogResponse
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string ApplicationName { get; set; }
        public string UserName { get; set; } = string.Empty; // Windows username
        public string MachineName { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty; // Unique machine identifier
        public string Action { get; set; } = string.Empty; // Install, Update, Uninstall
        public string Status { get; set; } = string.Empty; // Success, Failed, InProgress
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
        public string OldVersion { get; set; } = "0.0.0";
        public string NewVersion { get; set; } = string.Empty;
        public string InstallationPath { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public int DurationInSeconds { get; set; }
    }

    public class InstallationLogPaginationResponse
    {
        public List<InstallationLogResponse> Logs { get; set; } = new List<InstallationLogResponse>();
        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
