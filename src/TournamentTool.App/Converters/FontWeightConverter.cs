using System.Globalization;
using System.Windows.Data;
using TournamentTool.Domain.Enums;

namespace TournamentTool.App.Converters;

public class FontWeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not FontWeight fontWeight) return System.Windows.FontWeights.Normal;
        
        switch (fontWeight)
        {
            case FontWeight.Thin:
                return System.Windows.FontWeights.Thin;
            case FontWeight.Bold:
                return System.Windows.FontWeights.Bold;
            case FontWeight.SemiBold:
                return System.Windows.FontWeights.SemiBold;
            case FontWeight.Normal:
            default:
                return System.Windows.FontWeights.Normal;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not System.Windows.FontWeight wpfWeight) return FontWeight.Normal;
        if (wpfWeight == System.Windows.FontWeights.Thin)
            return FontWeight.Thin;
        if (wpfWeight == System.Windows.FontWeights.Bold)
            return FontWeight.Bold;
        if (wpfWeight == System.Windows.FontWeights.SemiBold)
            return FontWeight.SemiBold;
        
        return FontWeight.Normal;
    }
}