using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TournamentTool.App.Components.Buttons;

public partial class ButtonTaskbar : Button
{
    public static readonly DependencyProperty ColorBrushProperty = DependencyProperty.Register("ColorBrush", typeof(Brush), typeof(ButtonTaskbar), new PropertyMetadata(new SolidColorBrush(Colors.Black)));
    public Brush ColorBrush
    {
        get => (Brush)GetValue(ColorBrushProperty);
        set => SetValue(ColorBrushProperty, value);
    }

        
    public ButtonTaskbar()
    {
        InitializeComponent();
    }
}
