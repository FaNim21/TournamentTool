using System.Globalization;
using System.Windows.Data;
using TournamentTool.App.Rendering;
using TournamentTool.Domain.Entities.Markdown;

namespace TournamentTool.App.Converters;

public class MarkdownToTextBlockConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is List<MarkdownElement> elements)
            return MarkdownRenderer.RenderTextBlock(elements);
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class MarkdownToInlinesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is List<MarkdownElement> elements)
            return MarkdownRenderer.RenderInlines(elements);
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
