namespace TournamentTool.Core.Interfaces;

public enum CustomDispatcherPriority
{
    Invalid = -1,
    Inactive = 0,
    SystemIdle,
    ApplicationIdle,
    ContextIdle,
    Background,
    Input,
    Loaded,
    Render,
    DataBind,
    Normal,
    Send
}

public interface IDispatcherService
{
    bool CheckAccess();   
    void BeginInvoke(Action action, CustomDispatcherPriority priority = CustomDispatcherPriority.Normal);
    
    void Invoke(Action action, CustomDispatcherPriority priority = CustomDispatcherPriority.Normal);
    Task InvokeAsync(Action action, CustomDispatcherPriority priority = CustomDispatcherPriority.Normal);
    
    T Invoke<T>(Func<T> func, CustomDispatcherPriority priority = CustomDispatcherPriority.Normal);
    Task<T> InvokeAsync<T>(Func<T> func, CustomDispatcherPriority priority = CustomDispatcherPriority.Normal);
}