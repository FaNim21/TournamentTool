using System.Windows.Data;
using System;
using System.Globalization;

namespace TournamentTool.Converters;

[ValueConversion(typeof(bool), typeof(bool))]
public class BoolReverseConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool)
            return null;
        return !(bool)value;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}