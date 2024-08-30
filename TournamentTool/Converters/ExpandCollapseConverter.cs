using System.Globalization;
using System.Windows.Data;

namespace TournamentTool.Converters;

public class ExpandCollapseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isExpanded = (bool)value;
        return isExpanded ? "-" : "+";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
