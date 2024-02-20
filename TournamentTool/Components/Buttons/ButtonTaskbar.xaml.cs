using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace TournamentTool.Components.Buttons
{
    public partial class ButtonTaskbar : UserControl
    {
        public string ContentText
        {
            get => (string)GetValue(ContextTextProperty);
            set { SetValue(ContextTextProperty, value); }
        }
        public static readonly DependencyProperty ContextTextProperty = DependencyProperty.Register("ContentText", typeof(string), typeof(ButtonTaskbar), new PropertyMetadata(""));

        public Brush ColorBrush
        {
            get => (Brush)GetValue(ColorBrushProperty);
            set { SetValue(ColorBrushProperty, value); }
        }
        public static readonly DependencyProperty ColorBrushProperty = DependencyProperty.Register("ColorBrush", typeof(Brush), typeof(ButtonTaskbar), new PropertyMetadata(new SolidColorBrush(Colors.Black)));

        public event RoutedEventHandler? Click;

        public ButtonTaskbar()
        {
            InitializeComponent();
        }

        private void buttonClick(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(sender, e);
        }
    }
}
