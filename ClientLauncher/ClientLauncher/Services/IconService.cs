using ClientLauncher.Services.Interface;
using NLog;
using System.IO;
using System.Windows.Media.Imaging;

public class IconService : IIconService
{
    #region Init Constructor
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, string> _categoryIcons;
    private const string DefaultIcon = "pack://application:,,,/Assets/Icons/app_default.ico";

    public IconService()
    {
        // Map category to icon file
        _categoryIcons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Cage", "pack://application:,,,/Assets/Icons/app_cage.ico" },
                { "HTR", "pack://application:,,,/Assets/Icons/app_htr.ico" },
                { "Finance", "pack://application:,,,/Assets/Icons/app_finance.ico" }
            };

        Logger.Debug("IconService initialized with {Count} category mappings", _categoryIcons.Count);
    }
    #endregion

    #region Main Methods

    /// <summary>
    /// GetAppIcon
    /// </summary>
    /// <param name="iconUrl"></param>
    /// <param name="category"></param>
    /// <returns></returns>
    public BitmapImage GetAppIcon(string iconUrl, string category)
    {
        try
        {
            string iconPath = "";

            // Priority 1: Use IconUrl if provided and exists
            if (!string.IsNullOrEmpty(iconUrl))
            {
                // Check if it's a web URL
                if (iconUrl.StartsWith("http://") || iconUrl.StartsWith("https://"))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(iconUrl, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        Logger.Debug("Loaded icon from URL: {IconUrl}", iconUrl);
                        return bitmap;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Failed to load icon from URL: {IconUrl}", iconUrl);
                    }
                }
                // Check if it's a local file
                else if (File.Exists(iconUrl))
                {
                    iconPath = iconUrl;
                }
                // Try as pack URI
                else if (iconUrl.StartsWith("pack://"))
                {
                    iconPath = iconUrl;
                }
                else
                {
                    // Try to construct pack URI from relative path
                    iconPath = $"pack://application:,,,/Assets/Icons/{iconUrl}";
                }
            }
            // Priority 2: Use category icon
            else if (!string.IsNullOrEmpty(category) && _categoryIcons.ContainsKey(category))
            {
                iconPath = _categoryIcons[category];
                Logger.Debug("Using category icon for: {Category}", category);
            }
            // Priority 3: Use default icon
            else
            {
                iconPath = DefaultIcon;
                Logger.Debug("Using default icon");
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(uriString: iconPath, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            return image;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load icon, using default");

            // Return default icon on error
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
            catch
            {
                // Return null if even default fails
                return null!;
            }
        }
    }

    /// <summary>
    /// GetIconPath
    /// </summary>
    /// <param name="iconUrl"></param>
    /// <param name="category"></param>
    /// <returns></returns>
    public string GetIconPath(string iconUrl, string category)
    {
        if (!string.IsNullOrEmpty(iconUrl))
        {
            return iconUrl;
        }

        if (!string.IsNullOrEmpty(category) && _categoryIcons.ContainsKey(category))
        {
            return _categoryIcons[category];
        }

        return DefaultIcon;
    }

    /// <summary>
    /// Get actual file system path for icon (for creating shortcuts)
    /// Converts pack URI to physical file path
    /// </summary>
    /// <summary>
    /// Get actual file system path for icon (for creating shortcuts)
    /// Converts pack URI to physical file path
    /// </summary>
    public string? GetIconFilePath(string iconUrl, string category)
    {
        try
        {
            var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string packUri = string.Empty;

            // === SAME LOGIC AS GetAppIcon ===
            // Priority 1: Use IconUrl if provided
            if (!string.IsNullOrEmpty(iconUrl))
            {
                // Check if it's a web URL - we can't use web URLs for shortcuts
                if (iconUrl.StartsWith("http://") || iconUrl.StartsWith("https://"))
                {
                    Logger.Warn("Web URL cannot be used for shortcut icon, using category fallback");
                    // Fall through to category mapping
                }
                // Check if it's already a local file path
                else if (File.Exists(iconUrl))
                {
                    Logger.Debug("IconUrl is valid file path: {IconUrl}", iconUrl);
                    return iconUrl;
                }
                // Try as pack URI
                else if (iconUrl.StartsWith("pack://"))
                {
                    packUri = iconUrl;
                }
                // Try to construct pack URI from relative path (e.g., "app_cage.png")
                else
                {
                    packUri = $"pack://application:,,,/Assets/Icons/{iconUrl}";
                }
            }

            // Priority 2: Use category mapping (if packUri not set yet)
            if (string.IsNullOrEmpty(packUri))
            {
                if (!string.IsNullOrEmpty(category) && _categoryIcons.TryGetValue(category, out var categoryIcon))
                {
                    packUri = categoryIcon;
                    Logger.Debug("Using category icon for {Category}: {Icon}", category, packUri);
                }
                else
                {
                    // Priority 3: Use default icon
                    packUri = DefaultIcon;
                    Logger.Debug("Using default icon");
                }
            }

            // Convert pack URI to file path
            // pack://application:,,,/Assets/Icons/app_cage.ico -> Assets\Icons\app_cage.ico
            var relativePath = packUri
                .Replace("pack://application:,,,/", "")
                .Replace("/", "\\");

            var fullPath = Path.Combine(basePath ?? string.Empty, relativePath);

            if (File.Exists(fullPath))
            {
                Logger.Debug("Resolved icon file path: {FilePath}", fullPath);
                return fullPath;
            }
            else
            {
                Logger.Warn("Icon file not found at: {FilePath}", fullPath);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to get icon file path for category: {Category}", category);
            return null;
        }
    }
    #endregion
}