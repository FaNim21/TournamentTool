using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace TournamentTool.Converters;

public class EnumDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null!;

        var type = value.GetType();
        var memberInfo = type.GetMember(value.ToString() ?? string.Empty).FirstOrDefault();
        var displayAttr = memberInfo?.GetCustomAttribute<DisplayAttribute>();
        return displayAttr?.Name ?? value.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null!;
    }
}
