using System;
using System.Globalization;
using System.Windows.Data;

namespace TournamentTool.Converters;

public class EmptyStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
            return "empty";

        return value.ToString()!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
