using System.Windows;
using System.Windows.Threading;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.App.Services;

public class DispatcherService : IDispatcherService
{
    private readonly Dispatcher _dispatcher;
    private static readonly Dictionary<CustomDispatcherPriority, DispatcherPriority> PriorityMap = new()
    {
        { CustomDispatcherPriority.Invalid, DispatcherPriority.Invalid},
        { CustomDispatcherPriority.Inactive, DispatcherPriority.Inactive },
        { CustomDispatcherPriority.SystemIdle, DispatcherPriority.SystemIdle },
        { CustomDispatcherPriority.ApplicationIdle, DispatcherPriority.ApplicationIdle },
        { CustomDispatcherPriority.ContextIdle, DispatcherPriority.ContextIdle },
        { CustomDispatcherPriority.Loaded, DispatcherPriority.Loaded },
        { CustomDispatcherPriority.DataBind, DispatcherPriority.DataBind },
        { CustomDispatcherPriority.Background, DispatcherPriority.Background },
        { CustomDispatcherPriority.Input, DispatcherPriority.Input },
        { CustomDispatcherPriority.Normal, DispatcherPriority.Normal },
        { CustomDispatcherPriority.Render, DispatcherPriority.Render },
        { CustomDispatcherPriority.Send, DispatcherPriority.Send }
    };

    public DispatcherService()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }
    
    public bool CheckAccess()
    {
        return _dispatcher.CheckAccess();
    }
    public void BeginInvoke(Action action, CustomDispatcherPriority priority = CustomDispatcherPriority.Normal)
    {
        _dispatcher.BeginInvoke(action, MapPriority(priority));
    }

    public void Invoke(Action action, CustomDispatcherPriority priority = CustomDispatcherPriority.Normal)
    {
        if (_dispatcher.CheckAccess() && priority >= CustomDispatcherPriority.Normal)
        {
            action();
            return;
        }

        _dispatcher.Invoke(action, MapPriority(priority));
    }
    public async Task InvokeAsync(Action action, CustomDispatcherPriority priority = CustomDispatcherPriority.Normal)
    {
        if (_dispatcher.CheckAccess() && priority >= CustomDispatcherPriority.Normal)
        {
            action();
            return;
        }

        await _dispatcher.InvokeAsync(action, MapPriority(priority));
    }

    public T Invoke<T>(Func<T> func, CustomDispatcherPriority priority = CustomDispatcherPriority.Normal)
    {
        if (_dispatcher.CheckAccess() && priority >= CustomDispatcherPriority.Normal)
        {
            return func();
        }

        return _dispatcher.Invoke(func, MapPriority(priority));
    }
    public async Task<T> InvokeAsync<T>(Func<T> func, CustomDispatcherPriority priority = CustomDispatcherPriority.Normal)
    {
        if (_dispatcher.CheckAccess() && priority >= CustomDispatcherPriority.Normal)
        {
            return func();
        }

        return await _dispatcher.InvokeAsync(func, MapPriority(priority));
    }

    private DispatcherPriority MapPriority(CustomDispatcherPriority priority)
    {
        return PriorityMap.GetValueOrDefault(priority, DispatcherPriority.Normal);
    }
}