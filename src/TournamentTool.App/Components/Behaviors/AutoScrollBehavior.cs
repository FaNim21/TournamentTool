using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace TournamentTool.App.Components.Behaviors;

public class AutoScrollBehavior : Behavior<ItemsControl>
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(AutoScrollBehavior), new PropertyMetadata(true)); 
    public bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    private ScrollViewer? _scrollViewer;
    private bool _userScrolledUp;
    private const double ScrollingFactor = 0.2d;

    
    protected override void OnAttached()
    {
        base.OnAttached();
        
        AssociatedObject.Loaded += OnLoaded;
    }
    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.Loaded -= OnLoaded;

        if (_scrollViewer == null) return;
        
        _scrollViewer.ScrollChanged -= OnScrollChanged;
        _scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer = UIHelper.FindChild<ScrollViewer>(AssociatedObject);
        if (_scrollViewer == null) return;

        _scrollViewer.ScrollChanged += OnScrollChanged;
        _scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;

        ScrollToEndIfNeeded();
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (!IsEnabled || _scrollViewer == null) return;

        if (e.VerticalChange != 0)
            _userScrolledUp = IsUserScrolledUp();
        
        if (_scrollViewer.VerticalOffset >= _scrollViewer.ScrollableHeight - 1)
            _userScrolledUp = false;

        if (!_userScrolledUp && (e.ExtentHeightChange > 0 || e.ViewportHeightChange > 0))
            _scrollViewer.ScrollToEnd();
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!IsEnabled || _scrollViewer == null) return;

        _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - e.Delta * ScrollingFactor);

        e.Handled = true;
        _userScrolledUp = IsUserScrolledUp();
    }

    private bool IsUserScrolledUp()
    {
        if (_scrollViewer == null) return false;
        return _scrollViewer.VerticalOffset < _scrollViewer.ScrollableHeight;
    }

    private void ScrollToEndIfNeeded()
    {
        if (_userScrolledUp || _scrollViewer == null) return;
        _scrollViewer.ScrollToEnd();
    }
}