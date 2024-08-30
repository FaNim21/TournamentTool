using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TournamentTool.Converters;

public class IndentConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        int indentLevel = (int)values[0];
        return new Thickness(indentLevel * 40, 0, 0, 0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

