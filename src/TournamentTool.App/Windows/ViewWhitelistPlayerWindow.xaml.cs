using System.ComponentModel;
using System.Windows;

namespace TournamentTool.App.Windows;

public partial class ViewWhitelistPlayerWindow : Window
{
    public ViewWhitelistPlayerWindow()
    {
        InitializeComponent();
    }

    private void ExitButtonClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        ((IDisposable)DataContext).Dispose();
        base.OnClosing(e);
    }
}