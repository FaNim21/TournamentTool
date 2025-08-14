using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Interfaces;

namespace TournamentTool.Components.Behaviors;

public class ListBoxUpdateBehavior : BehaviorBase<ListBox>
{
    private static readonly List<ListBox> attachedListBoxes = [];

    public static readonly DependencyProperty DragAndDropContextProperty = DependencyProperty.Register( nameof(DragAndDropContext), typeof(IPovDragAndDropContext), typeof(ListBoxUpdateBehavior), new PropertyMetadata(null));
    public IPovDragAndDropContext? DragAndDropContext
    {
        get => (IPovDragAndDropContext?)GetValue(DragAndDropContextProperty);
        set => SetValue(DragAndDropContextProperty, value);
    }

    
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.SelectionChanged += OnSelectionChanged;
        attachedListBoxes.Add(AssociatedObject);
    }

    protected override void OnCleanup()
    {
        base.OnCleanup();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        attachedListBoxes.Remove(AssociatedObject);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AssociatedObject == null) return;
        UpdateAllListBoxesLayouts();

        if (DragAndDropContext?.CurrentChosenPOV == null) return;
               
        UnselectAllListBoxes();
        Keyboard.ClearFocus();
        DragAndDropContext.CurrentChosenPOV = null; 
    }

    private void UpdateAllListBoxesLayouts()
    {
        for (int i = 0; i < attachedListBoxes.Count; i++)
        {
            var listBox = attachedListBoxes[i];
            listBox.UpdateLayout();
        }
    }

    private void UnselectAllListBoxes()
    {
        for (int i = 0; i < attachedListBoxes.Count; i++)
        {
            var listBox = attachedListBoxes[i];
            listBox.UnselectAll();
        }
    }
}
