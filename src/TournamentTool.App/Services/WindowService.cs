using System.Windows;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Services.State;
using TournamentTool.ViewModels;

namespace TournamentTool.App.Services;

public class WindowService : IWindowService
{
    private readonly IApplicationState _applicationState;
    private readonly IInputController _inputController;
    private IDispatcherService Dispatcher { get; }
    
    private readonly Dictionary<Type, Window> _windows = new();
    private readonly Stack<Window> _dialogWindows = new();
    
    
    public WindowService(IApplicationState applicationState, IDispatcherService dispatcher, IInputController inputController)
    {
        _applicationState = applicationState;
        _inputController = inputController;
        Dispatcher = dispatcher;
    }

    public void Show<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null, string? windowTypeName = null) where TViewModel : BaseViewModel
    {
        ShowInternal(viewModel, false, onClosed, windowTypeName);
    }
    public void ShowDialog<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null, string? windowTypeName = null) where TViewModel : BaseViewModel
    {
        ShowInternal(viewModel, true, onClosed, windowTypeName);
    }

    private void ShowInternal<TViewModel>(TViewModel viewModel, bool modal, Action<TViewModel>? onClosed = null, string? windowTypeName = null) where TViewModel : BaseViewModel
    {
        var vmType = typeof(TViewModel);
        if (!modal && _windows.ContainsKey(vmType)) return;

        var window = CreateWindowForViewModel(viewModel, windowTypeName);
        if (window == null) return;

        void OnWindowClosed(object? sender, EventArgs e)
        {
            //TODO: 0 to przetestowac czy dobrze okna czysci
            window.Closed -= OnWindowClosed;
            
            if (modal)
            {
                _dialogWindows.Pop();
                
                if (_dialogWindows.Count > 0)
                    _dialogWindows.Peek().Activate();
                else
                    StopBlockingWindow();
            }
            else
            {
                _windows.Remove(vmType);
                _inputController.CleanupWindow(window);
            }
            
            if (viewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            onClosed?.Invoke(viewModel);
        }

        window.Closed += OnWindowClosed;

        if (modal)
        {
            window.Owner = _dialogWindows.Count > 0 ? _dialogWindows.Peek() : Application.Current.MainWindow!;
            _dialogWindows.Push(window);
            
            if (_dialogWindows.Count == 1) StartBlockingWindow();
            window.ShowDialog();
        }
        else
        {
            _windows[vmType] = window;
            _inputController.InitializeWindow(window);
            window.Show();
        }
    }
    
    public void Close<TViewModel>() where TViewModel : BaseViewModel
    {
        var vmType = typeof(TViewModel);
        if (!_windows.TryGetValue(vmType, out var window)) return;
        
        window.Close();
        _windows.Remove(vmType);
        StopBlockingWindow();
    }
    public bool IsOpen<TViewModel>() where TViewModel : BaseViewModel => _windows.ContainsKey(typeof(TViewModel));
    
    public void FocusMainWindow()
    {
        Application.Current.MainWindow?.Focus();
    }

    public void ShowLoading(Func<IProgress<float>, IProgress<string>, CancellationToken, Task> loading)
    {
        var viewModel = new LoadingWindowViewModel(loading, Dispatcher);
        ShowInternal(viewModel, true, null, "LoadingWindow");
    }

    private void StartBlockingWindow()
    {
        _applicationState.IsWindowBlocked = true;
    }
    private void StopBlockingWindow()
    {
        if (_windows.Count != 0) return;
        _applicationState.IsWindowBlocked = false;
    }

    private Window? CreateWindowForViewModel<TViewModel>(TViewModel viewModel, string? windowTypeName = null)
    {
        Type vmType = typeof(TViewModel);
        string windowName = !string.IsNullOrWhiteSpace(windowTypeName) ? 
            $"TournamentTool.App.Windows.{windowTypeName}" : 
            $"TournamentTool.App.Windows.{vmType.Name.Replace("ViewModel", "Window")}";
        Type? viewType = Type.GetType(windowName) ?? null;

        if (viewType == null || Activator.CreateInstance(viewType) is not Window window) return null;

        if (viewModel is ICloseRequest closeRequest)
        {
            closeRequest.RequestClose = window.Close;
        }
        window.DataContext = viewModel;
        window.Owner = Application.Current.MainWindow;
        return window;
    } 
    
    public void SetMainWindowTopMost(bool topMost)
    {
        Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow == null) return;
            Application.Current.MainWindow.Topmost = topMost;
        });
    }
    public void CloseApplication()
    {
        Dispatcher.Invoke(() =>
        {
            Application.Current.Shutdown();
        });
    }
}