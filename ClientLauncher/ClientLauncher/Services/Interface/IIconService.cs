using System.Windows.Media.Imaging;

namespace ClientLauncher.Services.Interface
{
    public interface IIconService
    {
        /// <summary>
        /// GetAppIcon
        /// </summary>
        /// <param name="iconUrl"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        BitmapImage GetAppIcon(string iconUrl, string category);
        /// <summary>
        /// GetIconPath
        /// </summary>
        /// <param name="iconUrl"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        string GetIconPath(string iconUrl, string category);
        /// <summary>
        /// GetIconFilePath
        /// </summary>
        /// <param name="iconUrl"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        string? GetIconFilePath(string iconUrl, string category);
    }
}