using System.Windows;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Services.State;
using TournamentTool.ViewModels;

namespace TournamentTool.App.Services;

public class WindowService : IWindowService
{
    private enum WindowType
    {
        Normal,
        Dialog,
        CustomDialog,
    }
    
    private record WindowEntryData(Window Window, BaseViewModel ViewModel, WindowType Type, Action CloseAction);
    
    private readonly IApplicationState _applicationState;
    private readonly IInputController _inputController;
    private IDispatcherService Dispatcher { get; }
    
    private readonly Dictionary<Type, WindowEntryData> _windows = new();
    private WindowEntryData? _lastCustomWindow;
    
    
    public WindowService(IApplicationState applicationState, IDispatcherService dispatcher, IInputController inputController)
    {
        _applicationState = applicationState;
        _inputController = inputController;
        Dispatcher = dispatcher;
    }

    public void Show<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null, string? windowTypeName = null) where TViewModel : BaseViewModel
    {
        if (WindowExists<TViewModel>()) return;
        
        Window? window = ShowInternal(viewModel, WindowType.Normal, onClosed, windowTypeName);
        window?.Show();
    }
    
    public void ShowDialog<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null, string? windowTypeName = null) where TViewModel : BaseViewModel
    {
        Window? window = ShowInternal(viewModel, WindowType.Dialog, onClosed, "DialogBoxWindow");
        if (window == null) return;
        
        window.Topmost = false;
        window.ShowInTaskbar = false;
        window.ResizeMode = ResizeMode.NoResize;
        StartBlockingWindow();
        
        window.ShowDialog();
    }
    public void ShowCustomDialog<TViewModel>(TViewModel viewModel, Action<TViewModel>? onClosed = null, string? windowTypeName = null) where TViewModel : BaseViewModel
    {
        if (WindowExists<TViewModel>()) return;
        
        Window? window = ShowInternal(viewModel, WindowType.CustomDialog, onClosed, windowTypeName);
        if (window == null) return;
        
        window.Topmost = false;
        window.ShowInTaskbar = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.ResizeMode = ResizeMode.NoResize;
        
        StartBlockingWindow();
        PinWindowToOwnerCenter(window);
        
        window.Show();
    }
    
    public void ShowLoading(Func<IProgress<float>, IProgress<string>, CancellationToken, Task> loading)
    {
        var viewModel = new LoadingWindowViewModel(loading, Dispatcher);
        
        ShowCustomDialog(viewModel, null, "LoadingWindow");
    }

    private Window? ShowInternal<TViewModel>(TViewModel viewModel, WindowType type, Action<TViewModel>? onClosed = null, string? windowTypeName = null) where TViewModel : BaseViewModel
    {
        Window? window = CreateWindowForViewModel(viewModel, windowTypeName);
        if (window == null) return null;

        _inputController.InitializeWindow(window);
        
        Type vmType = viewModel.GetType();
        window.Closed += OnWindowClosed;
        
        WindowEntryData windowEntry = new(window, viewModel, type, () => onClosed?.Invoke(viewModel));
        _windows.TryAdd(vmType, windowEntry);
        
        if (type == WindowType.CustomDialog)
        {
            _lastCustomWindow = windowEntry;
        }
        
        return window;
    }
    
    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        
        Type vmType = window.DataContext.GetType();
        if (!_windows.Remove(vmType, out var data)) return;

        _inputController.CleanupWindow(data.Window);
        
        switch (data.Type)
        {
            case WindowType.Normal: break;
            case WindowType.Dialog:
            case WindowType.CustomDialog:
                if (data == _lastCustomWindow)
                {
                    _lastCustomWindow = null;
                }
                
                if (_lastCustomWindow == null)
                {
                    StopBlockingWindow();
                }
                break;
        }
        
        data.Window.Closed -= OnWindowClosed;
        data.Window.DataContext = null;
            
        if (data.ViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }

        data.CloseAction.Invoke();
    }
    
    public void Close<TViewModel>() where TViewModel : BaseViewModel
    {
        var vmType = typeof(TViewModel);
        if (!_windows.TryGetValue(vmType, out var data)) return;
        
        data.Window.Close();
        _windows.Remove(vmType);
        StopBlockingWindow();
    }
    public bool IsOpen<TViewModel>() where TViewModel : BaseViewModel => _windows.ContainsKey(typeof(TViewModel));
    
    public void FocusMainWindow()
    {
        Application.Current.MainWindow?.Focus();
    }

    private void StartBlockingWindow()
    {
        _applicationState.IsWindowBlocked = true;
    }
    private void StopBlockingWindow()
    {
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
            closeRequest.RequestClose = window.Close;
        
        window.DataContext = viewModel;
        window.Owner = Application.Current.MainWindow;
        return window;
    } 
    
    private WindowEntryData? GetWindowEntry<TViewModel>()
    {
        Type type = typeof(TViewModel);
        if (!_windows.TryGetValue(type, out var entry) || entry == null) return null;
        
        return entry;
    }
    private bool WindowExists<TViewModel>()
    {
        Type type = typeof(TViewModel);
        return _windows.ContainsKey(type);
    }
    
    private void PinWindowToOwnerCenter(Window dialog)
    {
        if (dialog.Owner == null) return;

        Window owner = dialog.Owner;

        void UpdatePosition(object? _, EventArgs __)
        {
            dialog.Left = owner.Left + (owner.ActualWidth - dialog.ActualWidth) / 2;
            dialog.Top  = owner.Top  + (owner.ActualHeight - dialog.ActualHeight) / 2;
        }

        dialog.Loaded += UpdatePosition;
        owner.LocationChanged += UpdatePosition;
        owner.SizeChanged += UpdatePosition;

        dialog.Closed += (_, _) =>
        {
            dialog.Loaded -= UpdatePosition;
            owner.LocationChanged -= UpdatePosition;
            owner.SizeChanged -= UpdatePosition;
        };
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