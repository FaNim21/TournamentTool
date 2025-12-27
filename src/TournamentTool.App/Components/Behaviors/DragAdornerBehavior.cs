using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;

namespace TournamentTool.App.Components.Behaviors;

public static partial class DragAdornerBehavior
{
    private static DragAdorner? _dragAdorner;
    private static AdornerLayer? _adornerLayer;

    public static void StartDrag(UIElement element, DataTemplate? template = null)
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow == null) return;
        
        var root = (UIElement)mainWindow.Content;
        _adornerLayer = AdornerLayer.GetAdornerLayer(root);
        if (_adornerLayer == null) return;

        _dragAdorner = new DragAdorner(root, element);
        _adornerLayer.Add(_dragAdorner);
        
        element.GiveFeedback += Element_GiveFeedback;
        element.QueryContinueDrag += Element_QueryContinueDrag;
    }

    private static void Element_GiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
        if (_dragAdorner == null || _adornerLayer == null) return;
        if (sender is not UIElement element) return;

        (double positionX, double positionY) = GetMousePosition(element);
        _dragAdorner.SetPosition(positionX, positionY);
        _adornerLayer.Update(_dragAdorner.AdornedElement);
        
        e.UseDefaultCursors = true;
        e.Handled = true;
    }

    private static void Element_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
    {
        if (e.Action is not (DragAction.Drop or DragAction.Cancel)) return;
        
        EndDrag(sender as UIElement);
    }

    public static void EndDrag(UIElement? element)
    {
        if (_dragAdorner != null && _adornerLayer != null)
        {
            _adornerLayer.Remove(_dragAdorner);
            _dragAdorner = null;
        }

        if (element == null) return;
        
        element.GiveFeedback -= Element_GiveFeedback;
        element.QueryContinueDrag -= Element_QueryContinueDrag;
    }

    public static (double, double) GetMousePosition(UIElement element)
    {
        var window = Window.GetWindow(element);
        if (window == null) return (0, 0);
        
        Point screenPos = GetMouseScreenPosition();
        Point windowPos = window.PointFromScreen(screenPos);
        
        double positionX = windowPos.X - element.RenderSize.Width / 2;
        double positionY = windowPos.Y - element.RenderSize.Height / 2;
        
        return (positionX, positionY);
    }
    
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out POINT lpPoint);

    private struct POINT
    {
        public int X;
        public int Y;
    }
    
    private static Point GetMouseScreenPosition()
    {
        GetCursorPos(out POINT p);
        return new Point(p.X, p.Y);
    }
}