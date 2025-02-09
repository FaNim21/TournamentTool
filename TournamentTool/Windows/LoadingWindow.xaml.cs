using System.Windows;
using TournamentTool.ViewModels;

namespace TournamentTool.Windows;

public partial class LoadingWindow : Window
{
    private readonly LoadingWindowViewModel _viewModel;
    
    public LoadingWindow(Func<IProgress<float>, IProgress<string>, CancellationToken, Task> loading)
    {
        InitializeComponent();
        _viewModel = new LoadingWindowViewModel(loading);
        _viewModel.closeWindow += CloseWindow;
        
        DataContext = _viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _viewModel.closeWindow -= CloseWindow;
    }

    private void CloseWindow()
    {
        Dispatcher.Invoke(Close);
    }
}