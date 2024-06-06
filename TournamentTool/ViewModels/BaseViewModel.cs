using System.ComponentModel;
using System.Windows;

namespace TournamentTool.ViewModels;

public class BaseViewModel : INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;


    protected void OnPropertyChanged(string propertyName)
    {
        if (Application.Current == null) return;

        if (Application.Current.Dispatcher.CheckAccess())
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        else
            Application.Current.Dispatcher.Invoke(delegate { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); });
    }

    public virtual void OnEnable(object? parameter) { }
    public virtual bool OnDisable() { return true; }

    public virtual void Dispose() { }
}