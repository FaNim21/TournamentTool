using System.ComponentModel;
using System.Windows;
using TournamentTool.Utils;

namespace TournamentTool.Windows;
public partial class POVInformationWindow : Window
{
    public POVInformationWindow()
    {
        InitializeComponent();
        InputController.Instance.InitializeNewWindow(this);
    }

    private void ExitButtonClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        ((IDisposable)DataContext).Dispose();
        base.OnClosing(e);
    }
    protected override void OnClosed(EventArgs e)
    {
        InputController.Instance.CleanupWindow(this);
        base.OnClosed(e);
    }
}
