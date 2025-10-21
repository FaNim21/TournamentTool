using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.ViewModels.Menu;

public class ContextMenuViewModel : BaseViewModel
{
    public ObservableCollection<MenuItemViewModel> Items { get; } = [];

    
    public ContextMenuViewModel(IDispatcherService dispatcher) : base(dispatcher) { }
    
    public void AddItem(string header, ICommand command, bool isEnabled = true)
    {
        var item = new MenuItemViewModel(Dispatcher)
        {
            Header = header,
            Command = command,
            IsEnabled = isEnabled
        };
        Items.Add(item);
    }
    public void AddSeparator()
    {
        var item = new MenuItemViewModel(Dispatcher) { IsSeparator = true, };
        Items.Add(item);
    }
    
    public void AddSubmenu(string header, Action<ContextMenuViewModel> buildSubmenu)
    {
        var submenuItem = new MenuItemViewModel(Dispatcher) { Header = header };
        var submenu = new ContextMenuViewModel(Dispatcher);
        buildSubmenu(submenu);
        
        foreach (var item in submenu.Items)
        {
            submenuItem.Children.Add(item);
        }
        
        Dispatcher.Invoke(() => { Items.Add(submenuItem); });
    }
    
    public void Clear()
    {
        Dispatcher.Invoke(() => { Items.Clear(); });
    }
}