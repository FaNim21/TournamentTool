using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace TournamentTool.Components.Behaviors;

public class ListBoxSelectionBehavior : BehaviorBase<ListBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SelectionChanged += OnSelectionChanged;
    }
    protected override void OnCleanup()
    {
        base.OnCleanup();
        AssociatedObject.SelectionChanged -= OnSelectionChanged;
    }

    static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count <= 0) return;
        if (sender is not ListBox listBox) return;
        
        var valid = e.AddedItems[0];
        foreach (var item in new ArrayList(listBox.SelectedItems))
        {
            if (item != valid)
            {
                listBox.SelectedItems.Remove(item);
            }
        }
    }
}
