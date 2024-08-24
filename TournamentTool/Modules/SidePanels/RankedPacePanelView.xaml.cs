using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.Modules.SidePanels;

public partial class RankedPacePanelView : UserControl
{
    public RankedPacePanelView()
    {
        InitializeComponent();
    }

    private void ListBorder_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Border border) return;
        ((RankedPacePanel)DataContext).Controller.UnSelectItems(true);

        DragDrop.DoDragDrop(border, new DataObject(typeof(IPlayer), border.DataContext), DragDropEffects.Move);
    }

    private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject dependencyObject) return;

        var scrollViewer = Helper.FindAncestor<ScrollViewer>(dependencyObject);
        if (scrollViewer == null) return;

        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }
}
