using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Controller;

namespace TournamentTool.Views;

public partial class ControllerView : UserControl
{
    public ControllerView()
    {
        InitializeComponent();
    }

    private void CanvasBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Border border) return;

        DragDrop.DoDragDrop(border, border.DataContext, DragDropEffects.Move);
    }
    private void ListBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Border border) return;
        ((ControllerViewModel)DataContext).UnSelectItems(true);

        DragDrop.DoDragDrop(border, new DataObject(typeof(IPlayer), border.DataContext), DragDropEffects.Move);
    }

    private void Border_Drop(object sender, DragEventArgs e)
    {
        if (sender is not Border droppedBorder) return;
        if (DataContext is not ControllerViewModel controller) return;
        if (droppedBorder!.DataContext is not PointOfView pov) return;

        if (e.Data.GetData(typeof(IPlayer)) is IPlayer info)
        {
            pov.SetPOV(info);
        }
        else if (e.Data.GetData(typeof(PointOfView)) is PointOfView dragPov)
        {
            dragPov.Swap(pov);
        }

        if (string.IsNullOrEmpty(pov.TwitchName)) return;
        controller.UnSelectItems(true);
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is not ControllerViewModel viewModel) return;
        viewModel.ResizeCanvas();
    }

    private void CanvasBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ControllerViewModel viewModel) return;
        if (sender is not Border clickedBorder) return;
        if (clickedBorder!.DataContext is not PointOfView pov) return;

        viewModel.CurrentChosenPOV?.UnFocus();
        PointOfView? previousPOV = viewModel.CurrentChosenPOV;
        viewModel.CurrentChosenPOV = pov;
        viewModel.CurrentChosenPOV.Focus();

        if (viewModel.CurrentChosenPlayer == null)
        {
            if (previousPOV == null) return;

            viewModel.CurrentChosenPOV.Swap(previousPOV);
            viewModel.CurrentChosenPOV = null;
            return;
        }

        pov.SetPOV(viewModel.CurrentChosenPlayer);

        viewModel.CurrentChosenPOV.UnFocus();
        viewModel.UnSelectItems(true);
    }

    private void CanvasItem_RightMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ControllerViewModel) return;
        if (sender is not Border clickedBorder) return;
        if (clickedBorder!.DataContext is not PointOfView pov) return;

        pov.Clear();
    }

    private void CanvasItem_MouseEnter(object sender, MouseEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Hand;
    }
    private void CanvasItem_MouseLeave(object sender, MouseEventArgs e)
    {
        Mouse.OverrideCursor = null;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        //Trace.WriteLine("Wcisniety guzik");
        //TODO: 0 PROBLEMY Z CZYSZCZENIEM NA WCISNIECIU ESCAPE MOZE LEPIEJ ZROBIC INPUT CONTROLLER
        //NIC NIE CHCE DZIALAC MOZE GLOBAL HOTKEY STAD?  https://blog.magnusmontin.net/2015/03/31/implementing-global-hot-keys-in-wpf/
        if (e.Key == Key.Escape)
        {
            WhiteList.SelectedItem = null;
            WhiteList.UnselectAll();
            PaceMan.SelectedItem = null;
            PaceMan.UnselectAll();
            Keyboard.ClearFocus();
            if (DataContext is not ControllerViewModel viewModel) return;
            viewModel.UnSelectItems(true);
        }
    }

    private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PaceMan.UpdateLayout();
        WhiteList.UpdateLayout();

        if (DataContext is not ControllerViewModel viewModel) return;
        if (viewModel.CurrentChosenPOV != null)
        {
            PaceMan.UnselectAll();
            WhiteList.UnselectAll();
            Keyboard.ClearFocus();
            viewModel.CurrentChosenPOV = null;
        }
    }

    private void List_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
    }

    private void Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Slider slider) return;
        Point position = e.GetPosition(slider);

        double percentage = position.X / slider.ActualWidth;
        double newValue = slider.Minimum + (percentage * (slider.Maximum - slider.Minimum));

        slider.Value = newValue;
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
