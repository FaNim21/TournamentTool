using System.Collections.ObjectModel;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.ViewModels.UI;

public class TreeItemViewModel<T> : BaseViewModel where T : class
{
    public string Header
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public T Content { get; }
    
    public ObservableCollection<TreeItemViewModel<T>> SubItems { get; } = [];

    public TreeItemViewModel(IDispatcherService dispatcher, T content) : base(dispatcher)
    {
        Content = content;
    }
}