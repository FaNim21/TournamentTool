using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.Components.Behaviors;

public class ListBoxExtendSelection : BehaviorBase<ListBox>
{
    public static readonly DependencyProperty SynchronizedSelectedItemsProperty =
        DependencyProperty.RegisterAttached("SynchronizedSelectedItems",
            typeof(IList),
            typeof(ListBoxExtendSelection));

    public static void SetSynchronizedSelectedItems(DependencyObject obj, IList value)
    {
        obj.SetValue(SynchronizedSelectedItemsProperty, value);
    }
    public static IList GetSynchronizedSelectedItems(DependencyObject obj)
    {
        return (IList)obj.GetValue(SynchronizedSelectedItemsProperty);
    }
    
    private Player? _lastSelectedItem;

    
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject == null) return;
        
        AssociatedObject.PreviewMouseDown += OnPreviewMouseDown;
        AssociatedObject.SelectionChanged += OnSelectionChanged;
        AssociatedObject.KeyDown += OnKeyDown;
    }
    protected override void OnCleanup()
    {
        base.OnCleanup();
        if (AssociatedObject == null) return;
        
        AssociatedObject.PreviewMouseDown -= OnPreviewMouseDown;
        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        AssociatedObject.KeyDown -= OnKeyDown;
    }
    
    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not FrameworkElement { DataContext: Player item }) return;

        var listBox = (ListBox)sender;
        var selectedItems = GetSynchronizedSelectedItems(listBox);
        
        if (e.RightButton == MouseButtonState.Pressed)
        {
            selectedItems.Clear();
            listBox.SelectedItems.Clear();
            _lastSelectedItem = null;
            return;
        }
        
        if (e.LeftButton != MouseButtonState.Pressed) return;

        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            if (selectedItems.Contains(item))
            {
                selectedItems.Remove(item);
                listBox.SelectedItems.Remove(item);
            }
            else
            {
                selectedItems.Add(item);
                listBox.SelectedItems.Add(item);
                _lastSelectedItem = item;
            }

            e.Handled = true;
            return;
        }

        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (_lastSelectedItem == null)
            {
                e.Handled = true;
                return;
            }

            var allItems = listBox.Items.Cast<Player>().ToList();
            int start = allItems.IndexOf(_lastSelectedItem);
            int end = allItems.IndexOf(item);

            if (start == -1 || end == -1) return;

            int min = Math.Min(start, end);
            int max = Math.Max(start, end);

            for (int i = min; i <= max; i++)
            {
                var current = allItems[i];
                if (selectedItems.Contains(current)) continue;
                
                selectedItems.Add(current);
                listBox.SelectedItems.Add(current);
            }

            e.Handled = true;
            return;
        }

        if (selectedItems.Contains(item))
        {
            bool addClicked = selectedItems.Count > 1;
            
            selectedItems.Clear();
            listBox.SelectedItems.Clear();
            
            if (addClicked)
            {
                selectedItems.Add(item);
                listBox.SelectedItems.Add(item);
            }
            
            _lastSelectedItem = null;
            
            e.Handled = true;
            return;
        }
        
        _lastSelectedItem = item;
    }
    
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AssociatedObject == null) return;
        var selectedItems = GetSynchronizedSelectedItems(AssociatedObject);
        if (selectedItems == null) return;

        foreach (var item in e.AddedItems)
        {
            if (!selectedItems.Contains(item)) selectedItems.Add(item);
        }

        foreach (var item in e.RemovedItems)
        {
            if (selectedItems.Contains(item)) selectedItems.Remove(item);
        }
    }
    
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (AssociatedObject == null) return;
        var selectedItems = GetSynchronizedSelectedItems(AssociatedObject);
        
        if (selectedItems == null) return;
        if (e.Key != Key.Escape) return;
        
        selectedItems.Clear();
        AssociatedObject.SelectedItems.Clear();
        e.Handled = true;
    }
}