using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TournamentTool.Models;

namespace TournamentTool.Views;

public partial class PresetManagerView : UserControl
{
    [GeneratedRegex("[^0-9]+")]
    private static partial Regex NumberRegex();

    public ICommand OnListItemClickCommand
    {
        get { return (ICommand)GetValue(OnListItemClickCommandProperty); }
        set { SetValue(OnListItemClickCommandProperty, value); }
    }
    public static readonly DependencyProperty OnListItemClickCommandProperty = DependencyProperty.Register("OnListItemClickCommand", typeof(ICommand), typeof(PresetManagerView), new PropertyMetadata(null));


    public PresetManagerView()
    {
        InitializeComponent();
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        Regex regex = NumberRegex();
        e.Handled = regex.IsMatch(e.Text);
    }

    private void OnItemListClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListViewItem item) return;

        //Consts.IsSwitchingBetweenOpensInSettings = true;
        Keyboard.Focus((IInputElement)sender);
        OnListItemClickCommand?.Execute((Tournament)item.DataContext);
    }

    private void ListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
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
