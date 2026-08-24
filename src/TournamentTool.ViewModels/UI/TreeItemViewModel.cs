using System.Collections.ObjectModel;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.ViewModels.UI;

public class TreeItemViewModel<T> : BaseViewModel where T : class
{
    private string _header = string.Empty;
    public string Header
    {
        get => _header;
        set
        {
            _header = value;
            OnPropertyChanged();
        }
    }

    public T Content { get; }
    
    public ObservableCollection<TreeItemViewModel<T>> SubItems { get; } = [];

    public TreeItemViewModel(IDispatcherService dispatcher, T content) : base(dispatcher)
    {
        Content = content;
    }
}