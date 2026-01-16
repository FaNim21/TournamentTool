using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TournamentTool.App.Components.Buttons;

public partial class ButtonNormal : UserControl
{
    public static readonly DependencyProperty ContextTextProperty = DependencyProperty.Register("ContentText", typeof(string), typeof(ButtonNormal), new PropertyMetadata(""));
    public static readonly DependencyProperty OnCommandProperty = DependencyProperty.Register("OnCommand", typeof(ICommand), typeof(ButtonNormal), new PropertyMetadata(null));
    public static readonly DependencyProperty OnCommandParameterProperty = DependencyProperty.Register("OnCommandParameter", typeof(object), typeof(ButtonNormal), new PropertyMetadata(null));
    public static readonly DependencyProperty ContentMarginProperty = DependencyProperty.Register("ContentMargin", typeof(Thickness), typeof(ButtonNormal), new PropertyMetadata(null));
        
    public string ContentText
    {
        get => (string)GetValue(ContextTextProperty);
        set => SetValue(ContextTextProperty, value);
    }
    public ICommand OnCommand 
    { 
        get => (ICommand)GetValue(OnCommandProperty); 
        set => SetValue(OnCommandProperty, value); 
    }
    public object OnCommandParameter
    {
        get => GetValue(OnCommandParameterProperty);
        set => SetValue(OnCommandParameterProperty, value);
    }
    public Thickness ContentMargin
    {
        get => (Thickness)GetValue(ContentMarginProperty);
        set => SetValue(ContentMarginProperty, value);
    }

    public event RoutedEventHandler? Click;


    public ButtonNormal()
    {
        InitializeComponent();
    }

    private void ButtonClick(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(sender, e);
    }
}
