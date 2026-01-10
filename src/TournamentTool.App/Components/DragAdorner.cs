using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace TournamentTool.App.Components;

//drag adorner can cause very small cpu spike that i probably should ignore
public class DragAdorner : Adorner
{
    private const float _IMAGE_SCALE = 0.85f;
    
    protected override int VisualChildrenCount => 1;
    
    private readonly Point _startPoint;
    private Point _currentPoint;
    private readonly UIElement _element;
    private readonly FrameworkElement _container;

    private readonly Image _image;
    private Point _centerImageOffset; 

    
    public DragAdorner(FrameworkElement element, FrameworkElement container, Point positionInItem, Point startPoint) : base(element)
    {
        _element = element;
        _container = container;
        _startPoint = startPoint;

        ImageSource? source = CreateBitmap(element);
        _image = new Image
        {
            Source = source,
            IsHitTestVisible = false,
            RenderTransform = new ScaleTransform(_IMAGE_SCALE, _IMAGE_SCALE),
            Opacity = 0.4f,
            Effect = new DropShadowEffect { Opacity = 0.6f }
        };

        if (source == null) return;

        double imageWidth = source.Width * _IMAGE_SCALE;
        double imageHeight = source.Height * _IMAGE_SCALE;
        
        _centerImageOffset = new Point(imageWidth / 2 - positionInItem.X, imageHeight / 2 - positionInItem.Y);
        
        AddVisualChild(_image);
        
        _container.AllowDrop = true;
        _container.DragOver += HandleDragOver;
        _container.Drop += HandleDrop;
    }

    protected override Visual GetVisualChild(int index) => _image;
    protected override Size MeasureOverride(Size constraint)
    {
        _image.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return _image.DesiredSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        _image.Arrange(new Rect(_image.DesiredSize));
        return _image.DesiredSize;
    }
    
    public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
    {
        var transformGroup = new GeneralTransformGroup();
        transformGroup.Children.Add(base.GetDesiredTransform(transform)!);

        double offsetX = _currentPoint.X - _startPoint.X - _centerImageOffset.X;
        double offsetY = _currentPoint.Y - _startPoint.Y - _centerImageOffset.Y;
        transformGroup.Children.Add(new TranslateTransform(offsetX, offsetY));
        
        return transformGroup;
    }

    private void HandleDragOver(object sender, DragEventArgs e)
    {
        if (Parent == null) return;
        if (this.Parent is not AdornerLayer layer) return;
        
        _currentPoint = e.GetPosition(_container);
        layer.Update(_element);
    }
    private void HandleDrop(object sender, DragEventArgs e)
    {
        ((AdornerLayer)Parent)?.Remove(this);
        
        _container.DragOver -= HandleDragOver;
        _container.Drop -= HandleDrop;
    }
    
    private static ImageSource? CreateBitmap(FrameworkElement element)
    {
        if (!element.IsLoaded) return null;

        element.UpdateLayout();

        int width = (int)Math.Ceiling(element.ActualWidth);
        int height = (int)Math.Ceiling(element.ActualHeight);
        
        if (width <= 0 || height <= 0) return null;

        RenderTargetBitmap targetBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        targetBitmap.Render(element);
        targetBitmap.Freeze();
        
        return targetBitmap;
    }
}