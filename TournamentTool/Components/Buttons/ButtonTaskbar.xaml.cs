using System.Windows;
using System.Windows.Controls;


namespace TournamentTool.Components.Buttons
{
    public partial class ButtonTaskbar : UserControl
    {
        public string ContentText
        {
            get
            {
                return (string)GetValue(ContextTextProperty);
            }
            set
            {
                SetValue(ContextTextProperty, value);
            }
        }

        //NIE DZIALA TO
        public static readonly DependencyProperty ContextTextProperty = DependencyProperty.Register("ContentText", typeof(string), typeof(ButtonTaskbar), new PropertyMetadata(""));

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
