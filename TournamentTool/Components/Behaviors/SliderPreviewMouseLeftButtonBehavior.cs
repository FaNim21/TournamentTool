using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TournamentTool.Components.Behaviors;

public class SliderPreviewMouseLeftButtonBehavior : Behavior<Slider>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += OnPreviewMouseButtonDown;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseButtonDown;
    }

    private void OnPreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Slider slider) return;
        Point position = e.GetPosition(slider);

        double percentage = position.X / slider.ActualWidth;
        double newValue = slider.Minimum + (percentage * (slider.Maximum - slider.Minimum));

        slider.Value = newValue;
    }
}
