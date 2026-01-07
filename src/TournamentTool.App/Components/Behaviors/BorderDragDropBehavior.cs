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
    
    private Point _mouseDownPoint;
    private bool _dragging;
    

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObject_PreviewMouseLeftButtonDown;
        AssociatedObject.PreviewMouseMove += AssociatedObject_PreviewMouseMove;
    }

    protected override void OnCleanup()
    {
        base.OnCleanup();
        AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObject_PreviewMouseLeftButtonDown;
        AssociatedObject.PreviewMouseMove -= AssociatedObject_PreviewMouseMove;
    }

    private void AssociatedObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        _mouseDownPoint = e.GetPosition(AssociatedObject);
    }

    private void AssociatedObject_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragging) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        Point currentPoint = e.GetPosition(AssociatedObject);
        if (!(Math.Abs(currentPoint.X - _mouseDownPoint.X) >= SystemParameters.MinimumHorizontalDragDistance) &&
            !(Math.Abs(currentPoint.Y - _mouseDownPoint.Y) >= SystemParameters.MinimumVerticalDragDistance)) return;
        
        StartDragOperation(e);
    }

    private void StartDragOperation(MouseEventArgs e)
    {
        _dragging = true;
        
        if (AssociatedObject.DataContext is IPlayer { IsLive: false }) return;

        Window? window = AssociatedObject.GetAllAncestors().OfType<Window>().FirstOrDefault();
        if (window != null)
        {
            var container = window.Content as FrameworkElement;
            CreateAdorner(container!, e.GetPosition(container));
        }
        
        if (OnCommand != null && OnCommand.CanExecute(null))
        {
            OnCommand.Execute(null);
        }
        
        Type datatype = DragDataType ?? typeof(object);
        DataObject data = new DataObject(datatype, AssociatedObject.DataContext); 
        DragDrop.DoDragDrop(AssociatedObject, data, DragDropEffects.Move);
    }

    private void CreateAdorner(FrameworkElement container, Point startPoint)
    {
        DragAdorner adorner = new DragAdorner(AssociatedObject, container, startPoint);
        AdornerLayer.GetAdornerLayer(container)?.Add(adorner);
    }
}
