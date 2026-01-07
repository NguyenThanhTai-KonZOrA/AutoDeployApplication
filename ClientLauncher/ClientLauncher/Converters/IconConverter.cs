using System.Globalization;
using System.Windows.Data;

namespace ClientLauncher.Converters
{
    public class IconConverter : IMultiValueConverter
    {
        private static readonly IconService IconService = new IconService();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return null!;

            var iconUrl = values[0] as string ?? string.Empty;
            var category = values[1] as string ?? string.Empty;

            return IconService.GetAppIcon(iconUrl, category);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}