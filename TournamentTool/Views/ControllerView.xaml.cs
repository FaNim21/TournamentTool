using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Models;
using TournamentTool.ViewModels;

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

        DragDrop.DoDragDrop(border, new DataObject(typeof(ITwitchPovInformation), border.DataContext), DragDropEffects.Move);
    }

    private void Border_Drop(object sender, DragEventArgs e)
    {
        if (sender is not Border droppedBorder) return;
        if (DataContext is not ControllerViewModel controller) return;
        if (droppedBorder!.DataContext is not PointOfView pov) return;

        if (e.Data.GetData(typeof(ITwitchPovInformation)) is ITwitchPovInformation info)
        {
            pov.DisplayedPlayer = info.GetDisplayName();
            pov.TwitchName = info.GetTwitchName();
        }
        else if (e.Data.GetData(typeof(PointOfView)) is PointOfView dragPov)
        {
            dragPov.Swap(pov);
            controller.SetBrowserURL(dragPov!);
        }

        if (string.IsNullOrEmpty(pov.TwitchName)) return;
        pov.Update();
        controller.SetBrowserURL(pov);
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is not ControllerViewModel viewModel) return;
        viewModel.ResizeCanvas();
    }

    private void CanvasBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        //TODO: 0 zrobic zeby klikajac w ten sam element drugi raz zeby go odkliknac i to samo dla listboxes i dla canvas
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
            viewModel.SetBrowserURL(viewModel.CurrentChosenPOV);
            viewModel.SetBrowserURL(previousPOV!);
            previousPOV.Update();
            viewModel.CurrentChosenPOV = null;
            return;
        }

        pov.DisplayedPlayer = viewModel.CurrentChosenPlayer.GetDisplayName();
        pov.TwitchName = viewModel.CurrentChosenPlayer.GetTwitchName();
        pov.Update();
        viewModel.SetBrowserURL(pov!);
        viewModel.CurrentChosenPOV.UnFocus();
        viewModel.UnSelectItems(true);
    }

    private void CanvasItem_RightMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ControllerViewModel viewModel) return;
        if (sender is not Border clickedBorder) return;
        if (clickedBorder!.DataContext is not PointOfView pov) return;

        pov.Clear();
        viewModel.SetBrowserURL(pov);
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
        Trace.WriteLine("Wcisniety guzik");
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

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        Trace.WriteLine(e.Key.ToString());
    }
}
