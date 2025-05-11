using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Interfaces;
using TournamentTool.Modules.SidePanels;
using TournamentTool.ViewModels;

namespace TournamentTool.Components.Behaviors;

public class ListBoxUpdateBehavior : BehaviorBase<ListBox>
{
    private static readonly List<ListBox> attachedListBoxes = [];


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

        IPovDragAndDropContext? dragAndDropContext = AssociatedObject.DataContext as IPovDragAndDropContext;
        if (dragAndDropContext?.CurrentChosenPOV == null) return;
               
        UnselectAllListBoxes();
        Keyboard.ClearFocus();
        dragAndDropContext.CurrentChosenPOV = null; 
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
