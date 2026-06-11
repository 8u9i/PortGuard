using System.Globalization;
using System.Windows.Data;

namespace PortGuard.App.Converters;

public class NullToAllConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() ?? "All";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
