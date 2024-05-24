using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TournamentTool.Converters;

[ValueConversion(typeof(Enum), typeof(bool))]
public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value.Equals(parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? parameter : Binding.DoNothing;
    }
}
