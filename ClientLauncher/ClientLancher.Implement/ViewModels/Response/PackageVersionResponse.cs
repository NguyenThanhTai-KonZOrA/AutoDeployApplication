namespace ClientLancher.Implement.ViewModels.Response
{
    public class PackageVersionResponse
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string AppCode { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;
        public string PackageFileName { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }
        public string FileSizeFormatted => FormatFileSize(FileSizeBytes);
        public string FileHash { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;

        public string ReleaseNotes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsStable { get; set; }
        public string? MinimumClientVersion { get; set; }

        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public DateTime? PublishedAt { get; set; }

        public int DownloadCount { get; set; }
        public DateTime? LastDownloadedAt { get; set; }

        public int? ReplacesVersionId { get; set; }
        public string? ReplacesVersionNumber { get; set; }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}