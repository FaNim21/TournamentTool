using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.Views;

public partial class PresetManagerView : UserControl
{
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
        Regex regex = RegexPatterns.NumbersPattern();
        e.Handled = regex.IsMatch(e.Text);
    }

    private void OnItemListClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListViewItem item) return;

        Keyboard.Focus(item);
        OnListItemClickCommand?.Execute((TournamentPreset)item.DataContext);
    }

    private void ListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        ListViewItem? listViewItem = Helper.GetViewItemFromMousePosition<ListViewItem, ListView>(sender as ListView, e.GetPosition(sender as IInputElement));
        if (listViewItem == null) return;

        var contextMenu = (ContextMenu)FindResource("ListViewContextMenu");
        contextMenu.DataContext ??= DataContext;

        var currentItem = listViewItem.DataContext;
        foreach (var item in contextMenu.Items)
            if (item is MenuItem menuItem)
                menuItem.CommandParameter = currentItem;

        listViewItem.ContextMenu ??= contextMenu;
    }
    
    private void OpenDonateSite(object sender, RequestNavigateEventArgs e)
    {
        OpenKofiInBrowser(e.Uri.ToString());
    }
    private void OpenDonateSiteButton(object sender, RoutedEventArgs e)
    {
        OpenKofiInBrowser("https://ko-fi.com/fanim");
    }
    private void OpenKofiInBrowser(string uri)
    {
        var processStart = new ProcessStartInfo(uri)
        {
            UseShellExecute = true,
            Verb = "open"
        };
        Process.Start(processStart);
    }
}
