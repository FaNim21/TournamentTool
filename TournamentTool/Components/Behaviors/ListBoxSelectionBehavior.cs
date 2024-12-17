using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace TournamentTool.Components.Behaviors;

public class ListBoxSelectionBehavior : BehaviorBase<ListBox>
{
    public static readonly DependencyProperty ClickSelectionProperty = DependencyProperty.Register("ClickSelection", typeof(bool), typeof(ListBoxSelectionBehavior));


    public static bool GetClickSelection(DependencyObject obj)
    {
        return (bool)obj.GetValue(ClickSelectionProperty);
    }
    public static void SetClickSelection(DependencyObject obj, bool value)
    {
        obj.SetValue(ClickSelectionProperty, value);
    }

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
        if (e.AddedItems.Count > 0)
        {
            ListBox listBox = sender as ListBox;
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
}
