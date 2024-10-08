﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Utils;

namespace TournamentTool.Components.Behaviors;

public class ListBoxClampScrollBehavior : BehaviorBase<ListBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
    }

    protected override void OnCleanup()
    {
        base.OnCleanup();
        AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject dependencyObject) return;

        var scrollViewer = Helper.FindAncestor<ScrollViewer>(dependencyObject);
        if (scrollViewer == null) return;

        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }
}
