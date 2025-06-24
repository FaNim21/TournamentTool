using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Models;

namespace TournamentTool.Components.Behaviors;

public class BorderDragDropBehavior : BehaviorBase<Border>
{
    public static readonly DependencyProperty OnCommandProperty = DependencyProperty.Register(nameof(OnCommand), typeof(ICommand), typeof(BorderDragDropBehavior));
    public static readonly DependencyProperty DragDataTypeProperty = DependencyProperty.Register(nameof(DragDataType), typeof(Type), typeof(BorderDragDropBehavior));

    public ICommand OnCommand
    {
        get => (ICommand)GetValue(OnCommandProperty);
        set => SetValue(OnCommandProperty, value);
    }
    public Type DragDataType
    {
        get => (Type)GetValue(DragDataTypeProperty);
        set => SetValue(DragDataTypeProperty, value);
    }

    private Point _startPoint;
    private bool _isMouseDown;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseMove += OnMouseMove;
        AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
        AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    protected override void OnCleanup()
    {
        base.OnCleanup();
        AssociatedObject.MouseMove -= OnMouseMove;
        AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
        AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;
    }
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
        _isMouseDown = true;
    }
    
    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isMouseDown = false;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isMouseDown || e.LeftButton != MouseButtonState.Pressed) return;

        Point currentPosition = e.GetPosition(null);
        Vector diff = _startPoint - currentPosition;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        _isMouseDown = false;
        
        if (OnCommand != null && OnCommand.CanExecute(null))
        {
            OnCommand.Execute(null);
        }
        
        if (AssociatedObject.DataContext is IPlayer { IsLive: false }) return;
        var datatype = DragDataType ?? typeof(object);

        DragDrop.DoDragDrop(AssociatedObject, new DataObject(datatype, AssociatedObject.DataContext), DragDropEffects.Move);
    }
}
