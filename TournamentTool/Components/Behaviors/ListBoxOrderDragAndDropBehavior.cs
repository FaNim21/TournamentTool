using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Utils;

namespace TournamentTool.Components.Behaviors;

public class ListBoxOrderDragAndDropBehavior : BehaviorBase<ListBox>
{
    public static readonly DependencyProperty MoveItemCommandProperty = DependencyProperty.Register(nameof(MoveItemCommand), typeof(ICommand), typeof(ListBoxOrderDragAndDropBehavior));
    public ICommand MoveItemCommand
    {
        get => (ICommand)GetValue(MoveItemCommandProperty);
        set => SetValue(MoveItemCommandProperty, value);
    }
    
    private object? _draggedItem;
    private Point _dragStartPoint;
    
    
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += OnMouseDown;
        AssociatedObject.MouseMove += OnMouseMove;
        AssociatedObject.Drop += OnDrop;
        AssociatedObject.AllowDrop = true;
    }
    protected override void OnCleanup()
    {
        base.OnCleanup();
        AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseDown;
        AssociatedObject.MouseMove -= OnMouseMove;
        AssociatedObject.Drop -= OnDrop;
    }
    
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _draggedItem = Helper.GetItemUnderMouse<ListBoxItem>(AssociatedObject, e.GetPosition);
    }
    
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null) return;
        
        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            DragDrop.DoDragDrop(AssociatedObject, _draggedItem, DragDropEffects.Move);
        }
    }
    
    private void OnDrop(object sender, DragEventArgs e)
    {
        if (_draggedItem == null || MoveItemCommand == null) return;
    
        var targetItem = Helper.GetItemUnderMouse<ListBoxItem>(AssociatedObject, e.GetPosition);
        if (targetItem == null || targetItem == _draggedItem) return;
    
        if (AssociatedObject.ItemsSource is IList list)
        {
            int oldIndex = list.IndexOf(_draggedItem);
            int newIndex = list.IndexOf(targetItem);
    
            if (oldIndex != newIndex && MoveItemCommand.CanExecute((oldIndex, newIndex)))
            {
                MoveItemCommand.Execute((oldIndex, newIndex));
            }
        }
    
        _draggedItem = null;
    }
}