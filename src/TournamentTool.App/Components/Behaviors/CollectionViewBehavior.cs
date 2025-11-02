using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TournamentTool.App.Components.Behaviors;

public class CollectionViewBehavior : BehaviorBase<ItemsControl>
{
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(object), typeof(CollectionViewBehavior),
            new PropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty SortPropertyProperty =
        DependencyProperty.Register(nameof(SortProperty), typeof(string), typeof(CollectionViewBehavior),
            new PropertyMetadata(string.Empty, OnSortChanged));
    
    public static readonly DependencyProperty SortDirectionProperty =
        DependencyProperty.Register(nameof(SortDirection), typeof(ListSortDirection), typeof(CollectionViewBehavior),
            new PropertyMetadata(ListSortDirection.Descending, OnSortChanged));

    public static readonly DependencyProperty FilterProperty =
        DependencyProperty.Register(nameof(Filter), typeof(Predicate<object>), typeof(CollectionViewBehavior),
            new PropertyMetadata(null, OnFilterChanged));
    
    public static readonly DependencyProperty RefreshTriggerProperty =
        DependencyProperty.Register(nameof(RefreshTrigger), typeof(int), typeof(CollectionViewBehavior),
            new PropertyMetadata(0, OnRefreshTriggered));

    private ICollectionView? _view;

    public object? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }
    public Predicate<object>? Filter
    {
        get => (Predicate<object>?)GetValue(FilterProperty);
        set => SetValue(FilterProperty, value);
    }
    
    public string? SortProperty
    {
        get => (string?)GetValue(SortPropertyProperty);
        set => SetValue(SortPropertyProperty, value);
    }
    public ListSortDirection SortDirection
    {
        get => (ListSortDirection)GetValue(SortDirectionProperty);
        set => SetValue(SortDirectionProperty, value);
    }
    
    public int RefreshTrigger
    {
        get => (int)GetValue(RefreshTriggerProperty);
        set => SetValue(RefreshTriggerProperty, value);
    }
    
    
    protected override void OnAttached()
    {
        base.OnAttached();
        BuildCollectionView();
    }
    protected override void OnDetaching()
    {
        base.OnDetaching();
        _view = null;
        if (AssociatedObject != null) AssociatedObject.ItemsSource = null;
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((CollectionViewBehavior)d).BuildCollectionView();
    private static void OnSortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((CollectionViewBehavior)d).ApplySorting();
    private static void OnFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((CollectionViewBehavior)d).ApplyFilter();
    private static void OnRefreshTriggered(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((CollectionViewBehavior)d).RefreshView();

    private void BuildCollectionView()
    {
        if (Source == null || AssociatedObject == null) return;

        _view = CollectionViewSource.GetDefaultView(Source);
        ApplySorting();
        ApplyFilter();

        AssociatedObject.ItemsSource = _view;
    }

    private void ApplySorting()
    {
        if (_view == null || string.IsNullOrEmpty(SortProperty)) return;

        _view.SortDescriptions.Clear();
        _view.SortDescriptions.Add(new SortDescription(SortProperty, SortDirection));
    }
    private void ApplyFilter()
    {
        if (_view == null) return;
        _view.Filter = Filter;
    }
    
    private void RefreshView()
    {
        _view?.Refresh();
    }
}