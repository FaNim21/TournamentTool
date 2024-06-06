using System.Globalization;
using System.Windows.Data;
using TournamentTool.Models;

namespace TournamentTool.Converters;

public class GroupHeaderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CollectionViewGroup group && group.ItemCount > 0)
        {
            var firstItem = group.Items[0] as PaceMan;
            return firstItem?.SplitName!;
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
