using System.Collections.ObjectModel;
using System.Windows;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.SidePanels;

public class Group<T> : BaseViewModel where T : IGroupableItem
{
    private string _groupName = string.Empty;
    public string GroupName
    {
        get => _groupName;
        set
        {
            _groupName = value;
            OnPropertyChanged(nameof(GroupName));
        }
    }

    private int _groupSortOrder;
    public int GroupSortOrder
    {
        get => _groupSortOrder;
        set
        {
            _groupSortOrder = value;
            OnPropertyChanged(nameof(GroupSortOrder));
        }
    }

    private ObservableCollection<T> _items = [];
    public ObservableCollection<T> Items
    {
        get => _items;
        set
        {
            _items = value;
            OnPropertyChanged(nameof(Items));
        }
    }

    public Group(string groupName, int sortOrder)
    {
        GroupName = groupName;
        GroupSortOrder = sortOrder;
    }

    public void AddItem(T item)
    {
        Application.Current.Dispatcher.Invoke(() => Items.Add(item));
    }
    public void InsertItem(int index, T item)
    {
        Application.Current.Dispatcher.Invoke(() => Items.Insert(index, item));
    }
    public void RemoveItem(T item)
    {
        Application.Current.Dispatcher.Invoke(() => Items.Remove(item));
    }
    public void RemoveItemAt(int index)
    {
        Application.Current.Dispatcher.Invoke(() => Items.RemoveAt(index));
    }
}