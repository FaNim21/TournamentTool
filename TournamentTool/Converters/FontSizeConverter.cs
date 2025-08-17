using System.Globalization;
using System.Windows.Data;

namespace TournamentTool.Converters;

public class FontSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value?.ToString() is not { } width)
            return 12.0;

        double width2 = int.Parse(width);
        return Math.Max(6, width2 / 8);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null!;
    }
}
