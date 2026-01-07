using ClientLauncher.Models;
using ClientLauncher.Services.Interface;
using NLog;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace ClientLauncher.Services
{
    public class SelectiveUpdateService : ISelectiveUpdateService
    {
        private readonly IManifestService _manifestService;
        private readonly string _appBasePath = ConfigurationManager.AppSettings["AppsBasePath"] ?? @"C:\CompanyApps";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public SelectiveUpdateService(IManifestService manifestService)
        {
            _manifestService = manifestService;
        }

        /// <summary>
        /// Apply selective config update based on manifest rules
        /// </summary>
        public async Task<bool> ApplySelectiveConfigUpdateAsync(
            string appCode,
            ManifestDto manifest,
            string packagePath)
        {
            try
            {
                Logger.Info("Applying selective config update for {AppCode}", appCode);

                var configBasePath = Path.Combine(_appBasePath, appCode, "Config");
                var tempExtractPath = Path.Combine(Path.GetTempPath(), $"{appCode}_config_{Guid.NewGuid()}");

                // Extract package to temp location
                Directory.CreateDirectory(tempExtractPath);
                ZipFile.ExtractToDirectory(packagePath, tempExtractPath);

                var strategy = manifest.Config.MergeStrategy.ToLower();

                switch (strategy)
                {
                    case "replaceall":
                        await ReplaceAllConfigAsync(configBasePath, tempExtractPath);
                        break;

                    case "preservelocal":
                        await PreserveLocalConfigAsync(configBasePath, tempExtractPath);
                        break;

                    case "selective":
                        await ApplySelectiveUpdateAsync(configBasePath, tempExtractPath, manifest.Config.Files);
                        break;

                    case "merge":
                        await MergeConfigFilesAsync(configBasePath, tempExtractPath, manifest.Config.Files);
                        break;

                    default:
                        Logger.Warn("Unknown merge strategy: {Strategy}. Using preserveLocal", strategy);
                        await PreserveLocalConfigAsync(configBasePath, tempExtractPath);
                        break;
                }

                // Cleanup temp folder
                if (Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true);
                }

                Logger.Info("Selective config update completed for {AppCode}", appCode);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Selective config update failed for {AppCode}", appCode);
                return false;
            }
        }

        /// <summary>
        /// Replace all config files
        /// </summary>
        private async Task ReplaceAllConfigAsync(string configBasePath, string tempPath)
        {
            Logger.Info("Strategy: ReplaceAll - Replacing all config files");

            if (Directory.Exists(configBasePath))
            {
                Directory.Delete(configBasePath, true);
            }

            Directory.CreateDirectory(configBasePath);
            CopyDirectory(tempPath, configBasePath);
        }

        /// <summary>
        /// Only add new files, preserve existing
        /// </summary>
        private async Task PreserveLocalConfigAsync(string configBasePath, string tempPath)
        {
            Logger.Info("Strategy: PreserveLocal - Only adding new files");

            if (!Directory.Exists(configBasePath))
            {
                Directory.CreateDirectory(configBasePath);
                CopyDirectory(tempPath, configBasePath);
                return;
            }

            // Only copy files that don't exist locally
            foreach (var file in Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(tempPath, file);
                var targetPath = Path.Combine(configBasePath, relativePath);

                if (!File.Exists(targetPath))
                {
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    File.Copy(file, targetPath);
                    Logger.Debug("Added new file: {File}", relativePath);
                }
                else
                {
                    Logger.Debug("Preserved local file: {File}", relativePath);
                }
            }
        }

        /// <summary>
        /// Apply selective update based on file policies
        /// </summary>
        private async Task ApplySelectiveUpdateAsync(
            string configBasePath,
            string tempPath,
            List<ManifestDto.ConfigFilePolicy> filePolicies)
        {
            Logger.Info("Strategy: Selective - Applying file-specific policies");

            Directory.CreateDirectory(configBasePath);

            foreach (var policy in filePolicies)
            {
                var serverFilePath = Path.Combine(tempPath, policy.Name);
                var localFilePath = Path.Combine(configBasePath, policy.Name);

                if (!File.Exists(serverFilePath))
                {
                    Logger.Warn("Server file not found: {File}", policy.Name);
                    continue;
                }

                switch (policy.UpdatePolicy.ToLower())
                {
                    case "replace":
                        File.Copy(serverFilePath, localFilePath, overwrite: true);
                        Logger.Info("Replaced file: {File}", policy.Name);
                        break;

                    case "preserve":
                        if (!File.Exists(localFilePath))
                        {
                            File.Copy(serverFilePath, localFilePath);
                            Logger.Info("Added new file: {File}", policy.Name);
                        }
                        else
                        {
                            Logger.Info("Preserved local file: {File}", policy.Name);
                        }
                        break;

                    case "merge":
                        await MergeSingleFileAsync(serverFilePath, localFilePath, policy.Priority);
                        Logger.Info("Merged file: {File} (priority: {Priority})", policy.Name, policy.Priority);
                        break;

                    default:
                        Logger.Warn("Unknown policy: {Policy} for {File}", policy.UpdatePolicy, policy.Name);
                        break;
                }
            }
        }

        /// <summary>
        /// Merge JSON config files intelligently
        /// </summary>
        private async Task MergeConfigFilesAsync(
            string configBasePath,
            string tempPath,
            List<ManifestDto.ConfigFilePolicy> filePolicies)
        {
            Logger.Info("Strategy: Merge - Merging JSON config files");

            foreach (var policy in filePolicies.Where(p => p.UpdatePolicy.ToLower() == "merge"))
            {
                var serverFilePath = Path.Combine(tempPath, policy.Name);
                var localFilePath = Path.Combine(configBasePath, policy.Name);

                if (File.Exists(serverFilePath))
                {
                    await MergeSingleFileAsync(serverFilePath, localFilePath, policy.Priority);
                }
            }
        }

        /// <summary>
        /// Merge single JSON file with priority handling
        /// </summary>
        private async Task MergeSingleFileAsync(string serverFile, string localFile, string priority)
        {
            try
            {
                if (!File.Exists(serverFile))
                {
                    Logger.Warn("Server file not found for merge: {File}", serverFile);
                    return;
                }

                if (!File.Exists(localFile))
                {
                    File.Copy(serverFile, localFile);
                    Logger.Info("No local file exists, copied server file");
                    return;
                }

                // Read both files
                var serverJson = await File.ReadAllTextAsync(serverFile);
                var localJson = await File.ReadAllTextAsync(localFile);

                // Parse as JSON
                var serverObj = JsonSerializer.Deserialize<JsonElement>(serverJson);
                var localObj = JsonSerializer.Deserialize<JsonElement>(localJson);

                // Merge based on priority
                JsonElement mergedObj;
                if (priority.ToLower() == "server")
                {
                    mergedObj = MergeJson(serverObj, localObj); // Server overrides
                }
                else
                {
                    mergedObj = MergeJson(localObj, serverObj); // Local overrides
                }

                // Write merged result
                var mergedJson = JsonSerializer.Serialize(mergedObj, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(localFile, mergedJson);
                Logger.Info("Merged JSON file with {Priority} priority", priority);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to merge file: {File}", serverFile);
                // On error, fallback to copying server file
                File.Copy(serverFile, localFile, overwrite: true);
            }
        }

        /// <summary>
        /// Deep merge two JSON objects
        /// </summary>
        private JsonElement MergeJson(JsonElement primary, JsonElement secondary)
        {
            if (primary.ValueKind != JsonValueKind.Object || secondary.ValueKind != JsonValueKind.Object)
            {
                return primary;
            }

            var merged = new Dictionary<string, JsonElement>();

            // Add all from primary
            foreach (var prop in primary.EnumerateObject())
            {
                merged[prop.Name] = prop.Value;
            }

            // Merge from secondary (don't override if exists in primary)
            foreach (var prop in secondary.EnumerateObject())
            {
                if (!merged.ContainsKey(prop.Name))
                {
                    merged[prop.Name] = prop.Value;
                }
                else if (merged[prop.Name].ValueKind == JsonValueKind.Object &&
                         prop.Value.ValueKind == JsonValueKind.Object)
                {
                    // Recursive merge for nested objects
                    merged[prop.Name] = MergeJson(merged[prop.Name], prop.Value);
                }
            }

            return JsonSerializer.SerializeToElement(merged);
        }

        /// <summary>
        /// Copy directory recursively
        /// </summary>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
    }
}