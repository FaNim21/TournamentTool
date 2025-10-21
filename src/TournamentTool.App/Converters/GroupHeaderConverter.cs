using System.Globalization;
using System.Windows.Data;
using TournamentTool.Domain.Entities;

namespace TournamentTool.App.Converters;

public class GroupHeaderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CollectionViewGroup { ItemCount: > 0 } group) return string.Empty;
        var firstItem = group.Items[0] as IPace;
        return firstItem?.SplitName!;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null!;
    }
}
