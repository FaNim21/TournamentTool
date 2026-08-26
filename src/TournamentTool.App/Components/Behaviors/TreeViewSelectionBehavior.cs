using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TournamentTool.App.Components.Behaviors;

public class TreeViewSelectionBehavior : BehaviorBase<TreeView>
{
    public delegate bool IsChildOfPredicate(object nodeA, object nodeB);

    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(object),
        typeof(TreeViewSelectionBehavior), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    public static readonly DependencyProperty HierarchyPredicateProperty = DependencyProperty.Register(nameof(HierarchyPredicate), typeof(IsChildOfPredicate),
        typeof(TreeViewSelectionBehavior), new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty ExpandSelectedProperty = DependencyProperty.Register(nameof(ExpandSelected), typeof(bool),
        typeof(TreeViewSelectionBehavior), new FrameworkPropertyMetadata(false));

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }
    public bool ExpandSelected
    {
        get => (bool)GetValue(ExpandSelectedProperty);
        set => SetValue(ExpandSelectedProperty, value);
    }
    public IsChildOfPredicate HierarchyPredicate
    {
        get => (IsChildOfPredicate)GetValue(HierarchyPredicateProperty);
        set => SetValue(HierarchyPredicateProperty, value);
    }
    
    private readonly EventSetter _treeViewItemLoadedEventSetter;
    private bool _modelHandled;

    
    public TreeViewSelectionBehavior()
    {
        _treeViewItemLoadedEventSetter = new EventSetter(
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnTreeViewItemLoaded));
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.SelectedItemChanged += OnTreeViewSelectedItemChanged;
        AssociatedObject.PreviewMouseRightButtonDown += OnTreeViewPreviewMouseRightButtonDown;

        ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged +=
            OnTreeViewItemsChanged;

        UpdateTreeViewItemStyle();

        _modelHandled = true;
        UpdateAllTreeViewItems();
        _modelHandled = false;
    }
    protected override void OnCleanup()
    {
        base.OnCleanup();

        if (AssociatedObject == null)
            return;

        AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
        AssociatedObject.PreviewMouseRightButtonDown -= OnTreeViewPreviewMouseRightButtonDown;

        ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged -=
            OnTreeViewItemsChanged;

        AssociatedObject.ItemContainerStyle?.Setters.Remove(
            _treeViewItemLoadedEventSetter);
    }
    
    private static void OnSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        TreeViewSelectionBehavior behavior = (TreeViewSelectionBehavior)sender;
        if (behavior._modelHandled) return;
        if (behavior.AssociatedObject == null) return;

        behavior._modelHandled = true;
        behavior.UpdateAllTreeViewItems();
        behavior._modelHandled = false;
    }

    private void UpdateTreeViewItem(TreeViewItem item, bool recurse)
    {
        object model = item.DataContext;

        if (SelectedItem == null)
        {
            item.IsSelected = false;
        }
        else if (ReferenceEquals(SelectedItem, model))
        {
            if (!item.IsSelected)
                item.IsSelected = true;

            if (ExpandSelected)
                item.IsExpanded = true;
        }
        else if (HierarchyPredicate?.Invoke(SelectedItem, model) == true)
        {
            item.IsExpanded = true;
        }

        if (!recurse) return;

        foreach (var subitem in item.Items)
        {
            if (item.ItemContainerGenerator.ContainerFromItem(subitem) is not TreeViewItem tvi) continue;

            UpdateTreeViewItem(tvi, true);
        }
    }
    
    private void UpdateAllTreeViewItems()
    {
        TreeView treeView = AssociatedObject;
        foreach (var item in treeView.Items)
        {
            if (treeView.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem tvi) continue;
            
            UpdateTreeViewItem(tvi, true);
        }
    }
    
    private void UpdateTreeViewItemStyle()
    {
        if (AssociatedObject.ItemContainerStyle == null)
        {
            Style style = new Style(typeof(TreeViewItem), Application.Current.TryFindResource(typeof(TreeViewItem)) as Style);
            AssociatedObject.ItemContainerStyle = style;
        }

        SetterBaseCollection setters = AssociatedObject.ItemContainerStyle.Setters;

        if (!setters.Contains(_treeViewItemLoadedEventSetter))
        {
            setters.Add(_treeViewItemLoadedEventSetter);
        }
    }
    
    private void OnTreeViewItemsChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        UpdateAllTreeViewItems();
    }

    private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> args)
    {
        if (_modelHandled) return;

        SelectedItem = args.NewValue;
    }

    private void OnTreeViewItemLoaded(object sender, RoutedEventArgs args)
    {
        UpdateTreeViewItem((TreeViewItem)sender, false);
    }
    
    private void OnTreeViewPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs args)
    {
        if (args.OriginalSource is not DependencyObject source) return;

        TreeViewItem? item = FindTreeViewItem(source);
        if (item is null || item.IsSelected) return;

        _modelHandled = true;

        item.IsSelected = true;
        SelectedItem = item.DataContext;

        _modelHandled = false;
    }
    
    private static TreeViewItem? FindTreeViewItem(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is TreeViewItem item) return item;

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }
}