using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.SidePanels;

public abstract class GroupedPanelBase<TItem, TModel> : SidePanel where TItem : BaseViewModel, IGroupableItem, new() where TModel : class
{
    private ObservableCollection<Group<TItem>> _groups = [];
    public ObservableCollection<Group<TItem>> Groups
    {
        get => _groups;
        set
        {
            _groups = value;
            OnPropertyChanged(nameof(Groups));
        }
    }

    protected readonly List<TItem> _allItems = [];
    protected bool IsEmpty => _allItems.Count == 0;
    
    
    protected GroupedPanelBase(ControllerViewModel controller) : base(controller) { }

    public override bool OnDisable()
    {
        base.OnDisable();
        _allItems.Clear();
        Groups.Clear();
        SelectedPlayer = null;
        return true;
    }

    protected abstract TItem CreateViewModel(TModel model);

    protected void AddItem(TModel model)
    {
        var viewModel = CreateViewModel(model);
        var duplicate = _allItems.FirstOrDefault(i => i.Identifier == viewModel.Identifier);
        if (duplicate != null) return;

        _allItems.Add(viewModel);
        AddItemToGroup(viewModel);
    }
    protected void RemoveItem(string identifier)
    {
        var itemToRemove = _allItems.FirstOrDefault(i => i.Identifier == identifier);
        if (itemToRemove == null) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            _allItems.Remove(itemToRemove);
        });
        RemoveItemFromGroup(itemToRemove);
    }

    public virtual void Update()
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            foreach (var item in _allItems)
            {
                var oldGroupKey = item.GroupKey;
                var oldSortOrder = item.GroupSortOrder;
                var oldSortValue = item.SortValue;

                item.Update();

                if (item.GroupKey != oldGroupKey || item.GroupSortOrder != oldSortOrder)
                {
                    MoveItemToNewGroup(item, oldGroupKey, oldSortOrder);
                }
                else if (item.SortValue != oldSortValue)
                {
                    ResortItemInGroup(item);
                }
            }
        }, DispatcherPriority.Background);
    }

    protected void AddItemToGroup(TItem item)
    {
        var group = Groups.FirstOrDefault(g => 
            g.GroupName == item.GroupKey && 
            g.GroupSortOrder == item.GroupSortOrder);

        if (group == null)
        {
            group = new Group<TItem>(item.GroupKey, item.GroupSortOrder);

            // Sortowanie grup malejąco???????
            var insertIndex = 0;
            for (int i = 0; i < Groups.Count; i++)
            {
                if (Groups[i].GroupSortOrder < group.GroupSortOrder)
                {
                    insertIndex = i;
                    break;
                }
                insertIndex = i + 1;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Groups.Insert(insertIndex, group);
            });
        }

        InsertItemSorted(group, item);
    }

    protected void RemoveItemFromGroup(TItem item)
    {
        var group = Groups.FirstOrDefault(g => 
            g.GroupName == item.GroupKey && 
            g.GroupSortOrder == item.GroupSortOrder);
        
        if (group == null) return;

        group.RemoveItem(item);

        if (group.Items.Count == 0)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Groups.Remove(group);
            });
        }
    }

    protected void MoveItemToNewGroup(TItem item, string oldGroupKey, int oldSortOrder)
    {
        var oldGroup = Groups.FirstOrDefault(g => 
            g.GroupName == oldGroupKey && 
            g.GroupSortOrder == oldSortOrder);
        
        if (oldGroup != null)
        {
            oldGroup.RemoveItem(item);

            if (oldGroup.Items.Count == 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Groups.Remove(oldGroup);
                });
            }
        }

        AddItemToGroup(item);
    }

    protected void ResortItemInGroup(TItem item)
    {
        var group = Groups.FirstOrDefault(g => 
            g.GroupName == item.GroupKey && 
            g.GroupSortOrder == item.GroupSortOrder);
        
        if (group == null || group.Items.Count <= 1) return;

        var currentIndex = group.Items.IndexOf(item);
        if (currentIndex == -1) return;

        group.RemoveItemAt(currentIndex);
        InsertItemSorted(group, item);
    }

    protected void InsertItemSorted(Group<TItem> group, TItem item)
    {
        var index = 0;
        for (int i = 0; i < group.Items.Count; i++)
        {
            var comparison = item.SortValue.CompareTo(group.Items[i].SortValue);
            if (comparison < 0)
            {
                index = i;
                break;
            }

            if (comparison == 0)
            {
                if (string.Compare(item.SecondarySortValue, group.Items[i].SecondarySortValue, 
                    StringComparison.OrdinalIgnoreCase) < 0)
                {
                    index = i;
                    break;
                }
            }
            index = i + 1;
        }

        if (index >= group.Items.Count)
        {
            group.AddItem(item);
        }
        else
        {
            group.InsertItem(index, item);
        }
    }
}
