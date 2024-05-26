using System.Globalization;
using System.Windows.Data;

namespace TournamentTool.Converters;

[ValueConversion(typeof(bool), typeof(bool))]
public class NullToBoolConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
            return false;
        return (bool)value;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return false;
    }
}
