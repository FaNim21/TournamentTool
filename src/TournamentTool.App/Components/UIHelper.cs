using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TournamentTool.App.Components;

public static class UIHelper
{
    public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
    {
        if (depObj == null) yield break;
        
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            var child = VisualTreeHelper.GetChild(depObj, i);
            if (child is T typedChild)
                yield return typedChild;

            foreach (var childOfChild in FindVisualChildren<T>(child))
                yield return childOfChild;
        }
    }
    
    public static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;
        if (parent is T _child) return _child;

        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild)
            {
                return typedChild;
            }

            T? foundChild = FindChild<T>(child);
            if (foundChild != null) return foundChild;
        }

        return null;
    }

    public static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        do
        {
            if (current is T ancestor) return ancestor;
            current = VisualTreeHelper.GetParent(current);
        } while (current != null);
        return null;
    }

    public static T? GetFocusedUIElement<T>() where T : DependencyObject
    {
        IInputElement focusedControl = Keyboard.FocusedElement;
        T? result = FindChild<T>((DependencyObject)focusedControl);
        return result;
    }

    public static object? GetItemUnderMouse<TItem>(UIElement item, Func<IInputElement, Point> getPosition) where TItem : FrameworkElement
    {
        return GetUIItemUnderMouse<TItem>(item, getPosition)?.DataContext;
    }
    public static TItem? GetUIItemUnderMouse<TItem>(UIElement item, Func<IInputElement, Point> getPosition) where TItem : FrameworkElement
    {
        var pos = getPosition(item);
        var element = item.InputHitTest(pos) as DependencyObject;
    
        while (element != null && element is not ListBoxItem)
            element = VisualTreeHelper.GetParent(element);

        return element as TItem;
    }
    
    public static IEnumerable<FrameworkElement> GetAllAncestors(this FrameworkElement element)
    {
        while ((element = (VisualTreeHelper.GetParent(element) as FrameworkElement)!) != null)
        {
            yield return element;
        }
    }
}