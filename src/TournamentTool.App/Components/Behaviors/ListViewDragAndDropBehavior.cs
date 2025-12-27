using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TournamentTool.App.Components.Behaviors;

public class ListViewDragAndDropBehavior : BehaviorBase<ListView>
{
    public static readonly DependencyProperty ItemOrderChanged = DependencyProperty.Register(nameof(ItemOrderChangedCommand), typeof(ICommand), typeof(ListViewDragAndDropBehavior));
    public ICommand ItemOrderChangedCommand
    {
        get => (ICommand)GetValue(ItemOrderChanged);
        set => SetValue(ItemOrderChanged, value);
    }
    
    
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.MouseMove += OnMouseMove;
        AssociatedObject.DragOver += OnDragOver;
    }
    protected override void OnCleanup()
    {
        base.OnCleanup();
        
        AssociatedObject.MouseMove -= OnMouseMove;
        AssociatedObject.DragOver -= OnDragOver;
    }
    
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || sender is not ItemsControl itemsControl) return;
        if (e.OriginalSource is not DependencyObject originalSource) return;
        if (ItemsControl.ContainerFromElement(itemsControl, originalSource) is not ListViewItem item) return;
        
        DragDrop.DoDragDrop(item, new DataObject(DataFormats.Serializable, item.DataContext), DragDropEffects.Move);
    }
    
    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (sender is not ItemsControl itemsControl) return;
        if (e.OriginalSource is not DependencyObject originalSource) return;
        if (ItemsControl.ContainerFromElement(itemsControl, originalSource) is not ListViewItem item) return;

        object? targetedItem = item.DataContext;
        object? insertedItem = e.Data.GetData(DataFormats.Serializable);

        object?[] elements = [targetedItem, insertedItem];
        ItemOrderChangedCommand?.Execute(elements);
    }
}