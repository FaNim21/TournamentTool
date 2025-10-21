using System.Globalization;
using System.Windows.Data;
using TournamentTool.Domain.Entities.Input;
using TournamentTool.Domain.Enums;

namespace TournamentTool.App.Converters;

public class HotkeyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Hotkey hotkey) return string.Empty;
            
        string modifierString = hotkey.Modifiers == ModifierKeys.None ? "" : hotkey.Modifiers + " + ";
        return modifierString + hotkey.Key;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => string.Empty;
}
