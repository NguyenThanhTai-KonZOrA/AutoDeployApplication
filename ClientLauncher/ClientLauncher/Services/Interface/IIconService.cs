using System.Windows.Media.Imaging;

namespace ClientLauncher.Services.Interface
{
    public interface IIconService
    {
        BitmapImage GetAppIcon(string iconUrl, string category);
        string GetIconPath(string iconUrl, string category);
    }
}