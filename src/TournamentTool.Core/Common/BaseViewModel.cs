using System.ComponentModel;
using System.Runtime.CompilerServices;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.Core.Common;

public class BaseViewModel : INotifyPropertyChanged, IDisposable
{
    protected IDispatcherService Dispatcher { get; }
    
    public event PropertyChangedEventHandler? PropertyChanged;


    protected BaseViewModel(IDispatcherService dispatcher)
    {
        Dispatcher = dispatcher;
    }

    public virtual void OnEnable(object? parameter) { }
    public virtual bool OnDisable() => true;

    public virtual void Dispose() { }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (Dispatcher == null) return;

        if (Dispatcher.CheckAccess())
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        else
            Dispatcher.Invoke(delegate { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); });
    }
    
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}