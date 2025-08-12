using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Models;
using TournamentTool.Modules.OBS;

namespace TournamentTool.Components;

public partial class SceneCanvas : UserControl
{
    private readonly List<Border> _subscribedBorders = [];


    public SceneCanvas()
    {
        InitializeComponent();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        foreach (var border in _subscribedBorders)
        {
            border.ContextMenuOpening -= Border_ContextMenuOpening;
        }
        _subscribedBorders.Clear();
    }

    private void PointOfView_Drop(object sender, DragEventArgs e)
    {
        if (sender is not Border droppedBorder) return;
        if (DataContext is not Scene scene) return;
        if (droppedBorder.DataContext is not PointOfView pov) return;

        if (e.Data.GetData(typeof(IPlayer)) is IPlayer info)
        {
            pov.SetPOV(info);
        }
        else if (e.Data.GetData(typeof(PointOfView)) is PointOfView dragPov && dragPov.Scene.Type == pov.Scene.Type)
        {
            dragPov.Swap(pov);
        }

        if (string.IsNullOrEmpty(pov.DisplayedPlayer)) return;
        scene.SceneController.Controller.UnSelectItems(true);
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is not Scene viewModel) return;
        //viewModel.ResizeCanvas();
    }

    private void PointOfView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not Scene scene) return;
        if (sender is not Border clickedBorder) return;
        if (clickedBorder!.DataContext is not PointOfView pov) return;

        scene.OnPOVClick(pov);
    }
    private void PointOfView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;

        var contextMenu = (ContextMenu)FindResource("POVContextMenu");
        contextMenu.DataContext ??= DataContext;

        var currentItem = border.DataContext;
        foreach (var item in contextMenu.Items)
            if (item is MenuItem menuItem)
                menuItem.CommandParameter = currentItem;

        border.ContextMenu ??= contextMenu;

        if (!_subscribedBorders.Contains(border))
        {
            border.ContextMenuOpening += Border_ContextMenuOpening;
            _subscribedBorders.Add(border);
        }
    }

    private void Border_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not Border border) return;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (border.DataContext is not PointOfView pov) return;
            pov.Clear(true);
            e.Handled = true;
        }
    }

    private void PointOfView_MouseEnter(object sender, MouseEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Hand;
    }
    private void PointOfView_MouseLeave(object sender, MouseEventArgs e)
    {
        Mouse.OverrideCursor = null;
    }
}
