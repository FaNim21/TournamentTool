using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TournamentTool.Views;
public partial class PlayerManagerView : UserControl
{
    [GeneratedRegex("[^0-9]+")]
    private static partial Regex NumberRegex();

    public PlayerManagerView()
    {
        InitializeComponent();
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        Regex regex = NumberRegex();
        e.Handled = regex.IsMatch(e.Text);
    }

    private void List_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        ListViewItem? listViewItem = GetViewItemFromMousePosition<ListViewItem, ListView>(sender as ListView, e.GetPosition(sender as IInputElement));
        if (listViewItem == null) return;

        var contextMenu = (ContextMenu)FindResource("ListViewContextMenu");
        contextMenu.DataContext ??= DataContext;

        var currentItem = listViewItem.DataContext;
        foreach (var item in contextMenu.Items)
            if (item is MenuItem menuItem)
                menuItem.CommandParameter = currentItem;

        listViewItem.ContextMenu ??= contextMenu;
    }
    private T? GetViewItemFromMousePosition<T, U>(U? view, Point mousePosition) where T : Control where U : ItemsControl
    {
        HitTestResult hitTestResult = VisualTreeHelper.HitTest(view, mousePosition);
        DependencyObject? target = hitTestResult?.VisualHit;

        while (target != null && target is not T)
            target = VisualTreeHelper.GetParent(target);

        return target as T;
    }
}
