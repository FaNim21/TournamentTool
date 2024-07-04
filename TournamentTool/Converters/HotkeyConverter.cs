using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using TournamentTool.Utils;

namespace TournamentTool.Converters
{
    public class HotkeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Hotkey hotkey)
            {
                string modifierString = hotkey.ModifierKeys == ModifierKeys.None ? "" : hotkey.ModifierKeys.ToString() + " + ";
                return modifierString + hotkey.Key.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}
