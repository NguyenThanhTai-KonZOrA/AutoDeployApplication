using ClientLauncher.Services.Interface;
using NLog;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;

public class IconService : IIconService
{
    #region Init Constructor
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _iconCachePath;
    private const string DefaultIcon = "pack://application:,,,/Assets/Icons/app_default.ico";

    public IconService()
    {
        // Get base URL from config
        _baseUrl = ConfigurationManager.AppSettings["ClientLauncherBaseUrl"] ?? "http://10.21.10.1:8102";
        _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };

        var appsBasePath = ConfigurationManager.AppSettings["AppsBasePath"] ?? @"C:\CompanyApps";
        _iconCachePath = Path.Combine(appsBasePath, "Icons");

        // Create cache directory if not exists
        if (!Directory.Exists(_iconCachePath))
        {
            Directory.CreateDirectory(_iconCachePath);
            Logger.Info("Created icon cache directory: {CachePath}", _iconCachePath);
        }

        Logger.Debug("IconService initialized with base URL: {BaseUrl}", _baseUrl);
    }
    #endregion

    #region Main Methods

    /// <summary>
    /// GetAppIcon - Load icon from server or cache
    /// </summary>
    /// <param name="iconUrl">Relative URL path from server (e.g., "/uploads/icons/app_cage.ico")</param>
    /// <param name="category">Category fallback if iconUrl fails</param>
    /// <returns>BitmapImage for WPF display</returns>
    public BitmapImage GetAppIcon(string iconUrl, string category)
    {
        try
        {
            // Priority 1: Try to load from server using iconUrl
            if (!string.IsNullOrEmpty(iconUrl))
            {
                Logger.Debug("Attempting to load icon from URL: {IconUrl}", iconUrl);

                // Download icon from server (or get from cache)
                var iconFilePath = DownloadIconFromServer(iconUrl);

                if (!string.IsNullOrEmpty(iconFilePath) && File.Exists(iconFilePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(iconFilePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    Logger.Debug("Successfully loaded icon from cache: {FilePath}", iconFilePath);
                    return bitmap;
                }
            }

            // Priority 2: Fallback to default icon
            Logger.Debug("Using default icon");
            return LoadDefaultIcon();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load icon, using default");
            return LoadDefaultIcon();
        }
    }

    /// <summary>
    /// GetIconPath - Returns URI path for binding
    /// </summary>
    /// <param name="iconUrl">Server icon URL</param>
    /// <param name="category">Category fallback</param>
    /// <returns>URI string</returns>
    public string GetIconPath(string iconUrl, string category)
    {
        try
        {
            if (!string.IsNullOrEmpty(iconUrl))
            {
                var iconFilePath = DownloadIconFromServer(iconUrl);
                if (!string.IsNullOrEmpty(iconFilePath) && File.Exists(iconFilePath))
                {
                    return iconFilePath;
                }
            }

            return DefaultIcon;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to get icon path");
            return DefaultIcon;
        }
    }

    /// <summary>
    /// GetIconFilePath - Get physical file path for creating shortcuts
    /// </summary>
    /// <param name="iconUrl">Server icon URL</param>
    /// <param name="category">Category fallback</param>
    /// <returns>Physical file path or null</returns>
    public string? GetIconFilePath(string iconUrl, string category)
    {
        try
        {
            if (!string.IsNullOrEmpty(iconUrl))
            {
                Logger.Debug("Getting icon file path for: {IconUrl}", iconUrl);

                // Download from server (or get from cache)
                var iconFilePath = DownloadIconFromServer(iconUrl);

                if (!string.IsNullOrEmpty(iconFilePath) && File.Exists(iconFilePath))
                {
                    Logger.Debug("Resolved icon file path: {FilePath}", iconFilePath);
                    return iconFilePath;
                }
            }

            // Fallback to default icon from Assets
            var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var defaultIconPath = Path.Combine(basePath ?? string.Empty, "Assets", "Icons", "app_default.ico");

            if (File.Exists(defaultIconPath))
            {
                Logger.Debug("Using default icon file: {FilePath}", defaultIconPath);
                return defaultIconPath;
            }

            Logger.Warn("No icon file found");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to get icon file path");
            return null;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Download icon from server or return cached version
    /// </summary>
    /// <param name="iconUrl">Relative URL from server (e.g., "/uploads/icons/app.ico")</param>
    /// <returns>Local file path or null if failed</returns>
    private string? DownloadIconFromServer(string iconUrl)
    {
        try
        {
            // Generate cache file name from URL
            var fileName = Path.GetFileName(iconUrl);
            var cacheFilePath = Path.Combine(_iconCachePath, fileName);

            // Check if already cached
            if (File.Exists(cacheFilePath))
            {
                Logger.Debug("Icon found in cache: {CacheFile}", cacheFilePath);
                return cacheFilePath;
            }

            // Construct full URL: BaseUrl + iconUrl
            var fullUrl = iconUrl.StartsWith("http")
                ? iconUrl
                : $"{_baseUrl.TrimEnd('/')}/{iconUrl.TrimStart('/')}";

            Logger.Info("Downloading icon from: {FullUrl}", fullUrl);

            // Download icon synchronously (since we need it immediately for UI)
            var response = _httpClient.GetAsync(fullUrl).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn("Failed to download icon: {StatusCode}", response.StatusCode);
                return null;
            }

            var iconBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

            // Save to cache
            File.WriteAllBytes(cacheFilePath, iconBytes);
            Logger.Info("Icon downloaded and cached: {CacheFile}", cacheFilePath);

            return cacheFilePath;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to download icon from server: {IconUrl}", iconUrl);
            return null;
        }
    }

    /// <summary>
    /// Load default icon from Assets
    /// </summary>
    private BitmapImage LoadDefaultIcon()
    {
        try
        {
            var defaultImage = new BitmapImage();
            defaultImage.BeginInit();
            defaultImage.UriSource = new Uri(DefaultIcon, UriKind.Absolute);
            defaultImage.CacheOption = BitmapCacheOption.OnLoad;
            defaultImage.EndInit();
            defaultImage.Freeze();
            return defaultImage;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load default icon");
            return null!;
        }
    }

    #endregion
}