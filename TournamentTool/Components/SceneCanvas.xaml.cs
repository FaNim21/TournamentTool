using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Models;
using TournamentTool.Modules.OBS;

namespace TournamentTool.Components;

public partial class SceneCanvas : UserControl
{
    public SceneCanvas()
    {
        InitializeComponent();
    }

    private void CanvasBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Border border) return;

        DragDrop.DoDragDrop(border, border.DataContext, DragDropEffects.Move);
    }

    private void Border_Drop(object sender, DragEventArgs e)
    {
        if (sender is not Border droppedBorder) return;
        if (DataContext is not Scene scene) return;
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
        scene.Controller.UnSelectItems(true);
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is not Scene viewModel) return;
        //viewModel.ResizeCanvas();
    }

    private void CanvasBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not Scene scene) return;
        if (sender is not Border clickedBorder) return;
        if (clickedBorder!.DataContext is not PointOfView pov) return;

        scene.OnPOVClick(pov);
    }

    private void CanvasItem_RightMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not Scene) return;
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

}
