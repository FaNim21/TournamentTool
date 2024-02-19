using System;
using System.Globalization;
using System.Windows.Data;

namespace TournamentTool.Converters;

public class EnumToStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        if (!value.GetType().IsEnum)
            throw new InvalidOperationException("Converter only supports enum types.");

        return value.ToString();
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        if (!targetType.IsEnum)
            throw new InvalidOperationException("Converter only supports enum types.");

        return Enum.Parse(targetType, value.ToString()!);
    }
}
