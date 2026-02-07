using TournamentTool.Core.Common;

namespace TournamentTool.Core.Interfaces;

public interface IWindowService
{
    void ShowAtMouse<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null, string? windowTypeName = null);
    void Show<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null, string? windowTypeName = null);
    
    void ShowDialog<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null) where TViewModel : BaseWindowViewModel;
    void ShowCustomDialog<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null, string? windowTypeName = null) where TViewModel : BaseWindowViewModel;
    
    void Hide(Guid guid);
    void Close(Guid guid);
    bool IsOpen(Guid guid);

    void FocusMainWindow();

    void ShowLoading(Func<IProgress<float>, IProgress<string>, CancellationToken, Task> loading);

    void SetMainWindowTopMost(bool topMost);
    void CloseApplication();
}