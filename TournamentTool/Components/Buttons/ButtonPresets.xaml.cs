using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TournamentTool.Components.Buttons
{
    public partial class ButtonPresets : UserControl
    {
        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        public ICommand OnConsoleCommand
        {
            get { return (ICommand)GetValue(OnConsoleCommandProperty); }
            set { SetValue(OnConsoleCommandProperty, value); }
        }

        public object OnConsoleCommandParameter
        {
            get { return GetValue(OnConsoleCommandParameterProperty); }
            set { SetValue(OnConsoleCommandParameterProperty, value); }
        }

        public static readonly DependencyProperty ButtonTextProperty = DependencyProperty.Register("ButtonText", typeof(string), typeof(ButtonPresets), new PropertyMetadata(""));
        public static readonly DependencyProperty OnConsoleCommandProperty = DependencyProperty.Register("OnConsoleCommand", typeof(ICommand), typeof(ButtonPresets), new PropertyMetadata(null));
        public static readonly DependencyProperty OnConsoleCommandParameterProperty = DependencyProperty.Register("OnConsoleCommandParameter", typeof(object), typeof(ButtonPresets), new PropertyMetadata(null));

        public event RoutedEventHandler? Click;


        public ButtonPresets()
        {
            InitializeComponent();
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(sender, e);
        }
    }
}
