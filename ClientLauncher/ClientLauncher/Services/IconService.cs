using ClientLauncher.Services.Interface;
using NLog;
using System.IO;
using System.Windows.Media.Imaging;

public class IconService : IIconService
{
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
}