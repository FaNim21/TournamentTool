using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TournamentTool.App.Components;

public class DragAdorner : Adorner
{
    private readonly ContentPresenter _contentPresenter;
    private double _left;
    private double _top;
    
    protected override int VisualChildrenCount => 1;

    
    public DragAdorner(UIElement adornedElement, UIElement dragContent) : base(adornedElement)
    {
        IsHitTestVisible = false;
        
        var brush = new VisualBrush(dragContent)
        {
            Opacity = 0.7,
        };
        
        _contentPresenter = new ContentPresenter
        {
            Content = new Border
            {
                Width = dragContent.RenderSize.Width,
                Height = dragContent.RenderSize.Height,
                Background = brush
            },
            IsHitTestVisible = false
        };

        AddVisualChild(_contentPresenter);
    }

    public void SetPosition(double left, double top)
    {
        _left = left;
        _top = top;
        
        InvalidateArrange();
        InvalidateVisual();
    }

    protected override Visual GetVisualChild(int index) => _contentPresenter;

    protected override Size MeasureOverride(Size constraint)
    {
        _contentPresenter.Measure(constraint);
        return _contentPresenter.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _contentPresenter.Arrange(new Rect(finalSize));
        return finalSize;
    }
    
    public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
    {
        var result = new GeneralTransformGroup();
        result.Children.Add(base.GetDesiredTransform(transform)!);
        result.Children.Add(new TranslateTransform(_left, _top));
        return result;
    }
}