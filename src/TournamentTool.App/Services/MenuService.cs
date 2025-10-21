using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using TournamentTool.ViewModels.Menu;

namespace TournamentTool.App.Services;

public class MenuService : IMenuService
{
    public void ShowContextMenu(ContextMenuViewModel menuViewModel, object? placementTarget = null)
    {
        var contextMenu = CreateContextMenu(menuViewModel);
        if (placementTarget is FrameworkElement element)
        {
            contextMenu.PlacementTarget = element;
            contextMenu.Placement = PlacementMode.Top;
            contextMenu.VerticalOffset = -5;
        }
        
        ApplyStyles(contextMenu);
        
        if (contextMenu.Items.Count > 0) contextMenu.IsOpen = true;
    }
    public void ShowContextMenu(IContextMenuBuilder menuBuilder, object? placementTarget = null)
    {
        var menuViewModel = menuBuilder.BuildContextMenu();
        ShowContextMenu(menuViewModel, placementTarget);
    }
    
    private ContextMenu CreateContextMenu(ContextMenuViewModel viewModel)
    {
        var contextMenu = new ContextMenu();
        foreach (var item in viewModel.Items)
        {
            contextMenu.Items.Add(CreateMenuItem(item));
        }
        
        return contextMenu;
    }
    private object CreateMenuItem(MenuItemViewModel viewModel)
    {
        if (viewModel.IsSeparator)
        {
            var separator = new Separator();
            if (Application.Current.FindResource("MenuSeparatorStyle") is Style sepStyle)
            {
                separator.Style = sepStyle;
            }
            return separator;
        }
        
        var menuItem = new MenuItem
        {
            Header = viewModel.Header,
            Command = viewModel.Command,
            IsEnabled = viewModel.IsEnabled,
        };
        menuItem.SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(MenuItemViewModel.IsEnabled)) { Source = viewModel });
        
        foreach (var child in viewModel.Children)
        {
            menuItem.Items.Add(CreateMenuItem(child));
        }
        
        return menuItem;
    }
    
    private void ApplyStyles(ContextMenu contextMenu)
    {
        if (Application.Current.FindResource("StatusMenuStyle") is not Style menuStyle) return;
        contextMenu.Style = menuStyle;
    }
}