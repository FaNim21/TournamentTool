using TournamentTool.Core.Common;

namespace TournamentTool.Core.Interfaces;

public interface IWindowService
{
    void Show<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null, string? windowTypeName = null) where TViewModel : BaseViewModel;
    void Hide<TViewModel>() where TViewModel : BaseViewModel;
    
    void ShowDialog<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null, string? windowTypeName = null) where TViewModel : BaseViewModel;
    void ShowCustomDialog<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null, string? windowTypeName = null) where TViewModel : BaseViewModel;
    
    void Close<TViewModel>() where TViewModel : BaseViewModel;
    bool IsOpen<TViewModel>() where TViewModel : BaseViewModel;

    void FocusMainWindow();

    void ShowLoading(Func<IProgress<float>, IProgress<string>, CancellationToken, Task> loading);

    void SetMainWindowTopMost(bool topMost);
    void CloseApplication();
}