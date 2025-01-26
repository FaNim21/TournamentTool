using System.ComponentModel;
using System.Windows;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Windows;
public partial class WhitelistPlayerWindow : Window
{
    public WhitelistPlayerWindow(WhitelistPlayerWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.closeWindow += Close;
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

        ((WhitelistPlayerWindowViewModel)DataContext).closeWindow -= Close;
    }
}
