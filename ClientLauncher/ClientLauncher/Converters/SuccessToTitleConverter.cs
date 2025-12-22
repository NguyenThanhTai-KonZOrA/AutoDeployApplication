using System.Globalization;
using System.Windows.Data;

namespace ClientLauncher.Converters
{
    public class SuccessToTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool success)
            {
                return success ? "Installation Completed!" : "Installation Failed";
            }
            return "Unknown Status";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}