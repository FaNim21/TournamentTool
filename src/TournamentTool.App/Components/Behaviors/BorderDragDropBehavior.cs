using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using TournamentTool.Domain.Entities;

namespace TournamentTool.App.Components.Behaviors;


public class BorderDragDropBehavior : BehaviorBase<FrameworkElement>
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
        
        AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
        AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
        AssociatedObject.MouseMove += OnMouseMove;
    }
    protected override void OnCleanup()
    {
        base.OnCleanup();
        
        AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
        AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;
        AssociatedObject.MouseMove -= OnMouseMove;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isMouseDown = true;
        _startPoint = e.GetPosition(AssociatedObject);
    }
    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isMouseDown = false;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isMouseDown || e.LeftButton != MouseButtonState.Pressed) return;

        Point currentPoint = e.GetPosition(AssociatedObject);
        if (!(Math.Abs(currentPoint.X - _startPoint.X) >= SystemParameters.MinimumHorizontalDragDistance) &&
            !(Math.Abs(currentPoint.Y - _startPoint.Y) >= SystemParameters.MinimumVerticalDragDistance)) return;
        
        StartDragOperation(e);
    }

    private void StartDragOperation(MouseEventArgs e)
    {
        if (AssociatedObject.DataContext is IPlayer { IsLive: false }) return;

        Window? window = AssociatedObject.GetAllAncestors().OfType<Window>().FirstOrDefault();
        DragAdorner? adorner = null;
        FrameworkElement? container = null;

        if (window is {})
        {
            container = window.Content as FrameworkElement;
            if (container is {})
            {
                Point positionInWindow = e.GetPosition(container);
                Point positionInItem = e.GetPosition(AssociatedObject);
                
                adorner = new DragAdorner(AssociatedObject, container, positionInItem, positionInWindow);
                AdornerLayer.GetAdornerLayer(container)?.Add(adorner);
            }
        }

        if (OnCommand is {} && OnCommand.CanExecute(null))
        {
            OnCommand.Execute(null);
        }
        
        Type datatype = DragDataType ?? typeof(object);
        DataObject data = new DataObject(datatype, AssociatedObject.DataContext); 
        DragDrop.DoDragDrop(AssociatedObject, data, DragDropEffects.Move);

        if (adorner is {} && container is {})
        {
            _isMouseDown = false;
            AdornerLayer.GetAdornerLayer(container)?.Remove(adorner);
        }
    }
}
