using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Modules.Logging;

namespace TournamentTool.Views;

public partial class NotificationPanelView : UserControl
{
    public static readonly DependencyProperty OnHideCommandProperty = DependencyProperty.Register(nameof(OnHideCommand), typeof(ICommand), typeof(NotificationPanelView));
    public ICommand OnHideCommand
    {
        get => (ICommand)GetValue(OnHideCommandProperty);
        set => SetValue(OnHideCommandProperty, value);
    }
    
    public NotificationPanelView()
    {
        InitializeComponent();
    }

    private void BorderMouseDown(object sender, MouseButtonEventArgs e)
    {
        OnHideCommand.Execute(null);
    }
}