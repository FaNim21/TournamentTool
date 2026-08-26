using System.Globalization;
using System.Windows.Data;

namespace TournamentTool.App.Converters;

public class TypeEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter is not Type type) return false;

        return type.IsInstanceOfType(value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}