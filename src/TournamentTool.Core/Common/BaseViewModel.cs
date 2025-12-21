using System.ComponentModel;
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

    protected void OnPropertyChanged(string propertyName)
    {
        if (Dispatcher == null) return;

        if (Dispatcher.CheckAccess())
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        else
            Dispatcher.Invoke(delegate { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); });
    }
}