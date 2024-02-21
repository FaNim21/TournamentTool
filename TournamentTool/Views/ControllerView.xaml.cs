using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Views;

/*[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}*/

public partial class ControllerView : UserControl
{
    /*[DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    private DragAdorner dragAdorner;*/


    public ControllerView()
    {
        InitializeComponent();
    }

    private void Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;

        DragDrop.DoDragDrop(border, border.DataContext, DragDropEffects.Move);

        //dragAdorner = new DragAdorner(border);

        //var adornerLayer = AdornerLayer.GetAdornerLayer(this);

        //adornerLayer.Add(dragAdorner);

        // Start the drag operation with the adorner
        //DataObject data = new(typeof(Player), border.DataContext);
        //DragDrop.DoDragDrop(border, data, DragDropEffects.Copy);

        //adornerLayer.Remove(dragAdorner);
    }

    private void Border_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Border border) return;

        DragDrop.DoDragDrop(border, border.DataContext, DragDropEffects.Move);

        //dragAdorner = new(border);
        //var adornerLayer = AdornerLayer.GetAdornerLayer(this);
        //adornerLayer.Add(dragAdorner);

        // Start the drag operation with the adorner
        //DataObject data = new(typeof(Player), border.DataContext);
        //DragDrop.DoDragDrop(border, data, DragDropEffects.Copy);

        //adornerLayer.Remove(dragAdorner);
    }

    private void Border_Drop(object sender, DragEventArgs e)
    {
        //if (!e.Data.GetDataPresent(typeof(Player))) return;
        //if (e.Data.GetData(typeof(Player)) is not Player droppedPlayer) return;
        if (sender is not Border droppedBorder) return;
        if (DataContext is not ControllerViewModel controller) return;
        if (droppedBorder!.DataContext is not PointOfView pov) return;

        if (e.Data.GetData(typeof(Player)) is Player droppedPlayer)
        {
            pov.DisplayedPlayer = droppedPlayer.Name!;
            pov.Update();
            controller.SetBrowserURL(pov.SceneItemName!, droppedPlayer.TwitchName!);
        }
        else if (e.Data.GetData(typeof(PaceMan)) is PaceMan player)
        {
            pov.DisplayedPlayer = player.Nickname;
            pov.Update();
            controller.SetBrowserURL(pov.SceneItemName!, player.User.TwitchName!);
        }
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is not ControllerViewModel viewModel) return;
        viewModel.ResizeCanvas();
    }

    /*protected override void OnPreviewGiveFeedback(GiveFeedbackEventArgs e)
    {
        if (!GetCursorPos(out POINT cursorPos)) return;
        if (dragAdorner == null) return;

        Point pointRef = new Point(cursorPos.X, cursorPos.Y);
        Point relPos = PointFromScreen(pointRef);

        dragAdorner.CenterOffset = new Point(relPos.X - ActualWidth / 2, relPos.Y - ActualHeight / 2);

        Point position;
        position.X = relPos.X - startPoint.X;
        position.Y = relPos.Y - startPoint.Y;

        dragAdorner.Arrange(new Rect(position, dragAdorner.DesiredSize));
    }*/
}
