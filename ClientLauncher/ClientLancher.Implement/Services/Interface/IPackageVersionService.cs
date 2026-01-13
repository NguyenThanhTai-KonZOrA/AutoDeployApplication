using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.AspNetCore.Http;

public interface IPackageVersionService
{
    // Package Upload & Management
    Task<PackageVersionResponse> UploadPackageAsync(PackageUploadRequest request);
    Task<PackageVersionResponse> UpdatePackageAsync(PackageUpdateRequest request);
    Task<bool> DeletePackageAsync(int id);
    Task<PackageVersionResponse> PublishPackageAsync(PublishPackageRequest request);

    // Package Retrieval
    Task<PackageVersionResponse?> GetPackageByIdAsync(int id);
    Task<IEnumerable<PackageVersionResponse>> GetPackagesByApplicationIdAsync(int applicationId);
    Task<Dictionary<string, IEnumerable<PackageVersionResponse>>> GetAllPackagesGroupedByApplicationAsync();
    Task<PackageVersionResponse?> GetLatestVersionAsync(int applicationId, bool stableOnly = true);
    Task<IEnumerable<PackageVersionResponse>> GetVersionHistoryAsync(int applicationId, int take = 10);

    // Package Download
    Task<(byte[] fileData, string fileName, string contentType)> DownloadPackageAsync(int id);
    Task RecordDownloadStatisticAsync(int packageVersionId, string machineName, string userName,
        string ipAddress, bool success, long bytesDownloaded, int durationSeconds, string? error = null);

    // Rollback
    Task<PackageVersionResponse> RollbackToVersionAsync(int applicationId, string version, string performedBy);

    // Validation
    Task<bool> ValidatePackageAsync(IFormFile file);
    Task<string> CalculateFileHashAsync(Stream fileStream);
    Task UpdatePackageDownloadCountAsync(int packageId);
}