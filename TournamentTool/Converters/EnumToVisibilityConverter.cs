using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TournamentTool.Converters;

class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;

        string? enumValue = value.ToString();
        string? targetValue = parameter.ToString();

        if (enumValue!.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase))
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Visibility.Collapsed;
    }

}
