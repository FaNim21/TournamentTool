using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace TournamentTool.Utils;

public class DragAdorner : Adorner
{
    private readonly Brush renderBrush;
    private Rect renderRect;

    public Point CenterOffset;


    public DragAdorner(UIElement adornedElement) : base(adornedElement)
    {
        renderRect = new Rect(adornedElement.RenderSize);
        IsHitTestVisible = false;
        renderBrush = new SolidColorBrush(Color.FromRgb(150, 150, 50));
        CenterOffset = new Point(-renderRect.Width / 2, -renderRect.Height / 2);
    }
    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(renderBrush, null, renderRect);
    }
}