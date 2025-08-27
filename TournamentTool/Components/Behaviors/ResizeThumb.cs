using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.Components.Behaviors;

public class ResizeThumb : BehaviorBase<Thumb>
{
    public static readonly DependencyProperty MinWidthProperty = DependencyProperty.Register(nameof(MinWidth), typeof(double), typeof(ResizeThumb), new PropertyMetadata(426D));
    public static readonly DependencyProperty MaxWidthProperty = DependencyProperty.Register(nameof(MaxWidth), typeof(double), typeof(ResizeThumb), new PropertyMetadata(900D));
    public static readonly DependencyProperty MinHeightProperty = DependencyProperty.Register(nameof(MinHeight), typeof(double), typeof(ResizeThumb), new PropertyMetadata(240D));
    public static readonly DependencyProperty MaxHeightProperty = DependencyProperty.Register(nameof(MaxHeight), typeof(double), typeof(ResizeThumb), new PropertyMetadata(900D));
    public static readonly DependencyProperty InitialWidthProperty = DependencyProperty.Register(nameof(InitialWidth), typeof(float), typeof(ResizeThumb), new PropertyMetadata(426f));
    public static readonly DependencyProperty InitialHeightProperty = DependencyProperty.Register(nameof(InitialHeight), typeof(float), typeof(ResizeThumb), new PropertyMetadata(240f)); 
    public static readonly DependencyProperty ResizeCommandProperty = DependencyProperty.Register(nameof(ResizeCommand), typeof(ICommand), typeof(ResizeThumb), null);
    public double MinWidth
    {
        get => (double)GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }
    public double MaxWidth
    {
        get => (double)GetValue(MaxWidthProperty);
        set => SetValue(MaxWidthProperty, value);
    }
    public double MinHeight
    {
        get => (double)GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }
    public double MaxHeight
    {
        get => (double)GetValue(MaxHeightProperty);
        set => SetValue(MaxHeightProperty, value);
    }
    public float InitialWidth
    {
        get => (float)GetValue(InitialWidthProperty);
        set => SetValue(InitialWidthProperty, value);
    }
    public float InitialHeight
    {
        get => (float)GetValue(InitialHeightProperty);
        set => SetValue(InitialHeightProperty, value);
    } 
    public ICommand? ResizeCommand
    {
        get => (ICommand)GetValue(ResizeCommandProperty);
        set => SetValue(ResizeCommandProperty, value);
    }

    private RowDefinition? _targetRow;
    private ColumnDefinition? _targetColumn;
    

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.DragDelta += OnDragDelta;
        AssociatedObject.Loaded += OnThumbLoaded;
    }
    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.DragDelta -= OnDragDelta;
    }
    
    private void OnThumbLoaded(object sender, RoutedEventArgs e)
    {
        AssociatedObject.Loaded -= OnThumbLoaded;
        InitializeAndApplyDimensions();
    } 
    
    private void InitializeAndApplyDimensions()
    {
        var thumb = AssociatedObject;
        var gridToResize = thumb.Parent as Grid;

        if (gridToResize?.Parent is not Grid parentGrid) return;
        _targetRow = parentGrid.RowDefinitions[0];

        if (parentGrid.Parent is not Grid mainGrid) return;
        _targetColumn = mainGrid.ColumnDefinitions[0];
                
        _targetColumn.Width = new GridLength(InitialWidth, GridUnitType.Pixel);
        _targetRow.Height = new GridLength(InitialHeight, GridUnitType.Pixel);
    }

    private void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_targetRow == null || _targetColumn == null) return;
        
        double newWidth = _targetColumn.ActualWidth + e.HorizontalChange;
        newWidth = Math.Max(MinWidth, Math.Min(newWidth, MaxWidth));
        double newHeight = newWidth / Consts.AspectRatio;
            
        if (newHeight > MaxHeight)
        {
            newHeight = MaxHeight;
            newWidth = newHeight * Consts.AspectRatio;
        }
        else if (newHeight < MinHeight)
        {
            newHeight = MinHeight;
            newWidth = newHeight * Consts.AspectRatio;
        }
                
        _targetColumn.Width = new GridLength(newWidth, GridUnitType.Pixel);
        _targetRow.Height = new GridLength(newHeight, GridUnitType.Pixel);
                
        ResizeCommand?.Execute(new Dimension((float)newWidth, (float)newHeight));
    }
}