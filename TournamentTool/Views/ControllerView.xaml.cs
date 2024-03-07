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

    private void Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;

        DragDrop.DoDragDrop(border, border.DataContext, DragDropEffects.Move);
    }
    private void Border_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Border border) return;

        DragDrop.DoDragDrop(border, border.DataContext, DragDropEffects.Move);
    }

    private void Border_Drop(object sender, DragEventArgs e)
    {
        if (sender is not Border droppedBorder) return;
        if (DataContext is not ControllerViewModel controller) return;
        if (droppedBorder!.DataContext is not PointOfView pov) return;

        if (e.Data.GetData(typeof(Player)) is Player droppedPlayer)
        {
            pov.DisplayedPlayer = droppedPlayer.Name!;
            pov.TwitchName = droppedPlayer.TwitchName!;
        }
        else if (e.Data.GetData(typeof(PaceMan)) is PaceMan player)
        {
            pov.DisplayedPlayer = player.Nickname;
            pov.TwitchName = player.User.TwitchName;
        }
        else if (e.Data.GetData(typeof(PointOfView)) is PointOfView dragPov)
        {
            dragPov.Swap(pov);
            controller.SetBrowserURL(dragPov.SceneItemName!, dragPov.TwitchName);
        }

        pov.Update();
        controller.SetBrowserURL(pov.SceneItemName!, pov.TwitchName);
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is not ControllerViewModel viewModel) return;
        viewModel.ResizeCanvas();
    }
}
