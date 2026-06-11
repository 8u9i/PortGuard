using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PortGuard.App.Converters;

public class AdminColorConverter : IValueConverter
{
    public static readonly AdminColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1))
            : new SolidColorBrush(Color.FromRgb(0xF3, 0x8B, 0xA8));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
