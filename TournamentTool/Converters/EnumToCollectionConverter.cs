using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Markup;
using TournamentTool.Utils;

namespace TournamentTool.Converters;

[ValueConversion(typeof(Enum), typeof(IEnumerable<ValueDescription>))]
public partial class EnumToCollectionConverter : MarkupExtension, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;

        Type enumType = value.GetType();
        if (!enumType.IsEnum) return null;

        return Enum.GetValues(enumType)
            .Cast<Enum>()
            .Select(e => new { Value = e, DisplayName = SplitCamelCase(e.ToString()) })
            .ToList();
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
    
    private static string SplitCamelCase(string input)
    {
        return MyRegex().Replace(input, " $1");
    }

    [GeneratedRegex("(\\B[A-Z])")]
    private static partial Regex MyRegex();
}
