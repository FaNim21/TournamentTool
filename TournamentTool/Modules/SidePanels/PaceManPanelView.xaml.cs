using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.Modules.SidePanels;

public partial class PaceManPanelView : UserControl
{
    public PaceManPanelView()
    {
        InitializeComponent();
    }

    private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //paceMan.UpdateLayout();

        if (DataContext is not PaceManPanel viewModel) return;
        if (viewModel.Controller.CurrentChosenPOV != null)
        {
            //paceMan.UnselectAll();
            Keyboard.ClearFocus();
            viewModel.Controller.CurrentChosenPOV = null;
        }
    }

    private void ListBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Border border) return;
        ((PaceManPanel)DataContext).Controller.UnSelectItems(true);

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

    private void List_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
    }
}
