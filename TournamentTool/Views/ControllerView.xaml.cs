using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Commands;
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
            controller.SetBrowserURL(dragPov.SceneItemName!, dragPov.TwitchName);
        }

        if (string.IsNullOrEmpty(pov.TwitchName)) return;
        pov.Update();
        controller.SetBrowserURL(pov.SceneItemName!, pov.TwitchName);
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
        if (viewModel.CurrentChosenPlayer == null) return;

        pov.DisplayedPlayer = viewModel.CurrentChosenPlayer.GetDisplayName();
        pov.TwitchName = viewModel.CurrentChosenPlayer.GetTwitchName();
        pov.Update();
        viewModel.SetBrowserURL(pov.SceneItemName!, pov.TwitchName);
        viewModel.SelectedWhitelistPlayer = null;
    }
}
