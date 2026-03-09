using System.Windows;
using Microsoft.Xaml.Behaviors;
using TournamentTool.Domain.Entities;

namespace TournamentTool.App.Components.Behaviors;

public class ActualSizeBehavior : Behavior<FrameworkElement>
{
    public static readonly DependencyProperty DimensionProperty = DependencyProperty.Register( nameof(Dimension), typeof(Dimension), typeof(ActualSizeBehavior),
        new FrameworkPropertyMetadata(new Dimension(-1, -1), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    
    public static readonly DependencyProperty WidthProperty = DependencyProperty.Register( nameof(Width), typeof(double), typeof(ActualSizeBehavior),
        new FrameworkPropertyMetadata(-1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty HeightProperty = DependencyProperty.Register( nameof(Height), typeof(double), typeof(ActualSizeBehavior),
        new FrameworkPropertyMetadata(-1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    
    public Dimension Dimension
    {
        get => (Dimension)GetValue(DimensionProperty);
        set => SetValue(DimensionProperty, value);
    }
    public double Width
    {
        get => (double)GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }
    public double Height
    {
        get => (double)GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    protected override void OnAttached()
    {
        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.SizeChanged += OnSizeChanged;
    }
    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnLoaded;
        AssociatedObject.SizeChanged -= OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateSize();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateSize();
    }

    private void UpdateSize()
    {
        Dimension = new Dimension((float)AssociatedObject.ActualWidth - 2, (float)AssociatedObject.ActualHeight - 2);
        
        Width = AssociatedObject.ActualWidth;
        Height = AssociatedObject.ActualHeight;
    }
}