using System.Globalization;
using System.Windows.Data;

namespace TournamentTool.Converters;

public class TypeToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        Type? target = parameter as Type;
        return target != null && target.IsInstanceOfType(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return false;
    }
}
