using System.Windows;
using System.Windows.Input;

namespace TournamentTool.App.Components.Behaviors.General;

public class MouseLeftButtonDownBehavior  : BehaviorBase<FrameworkElement>
{
    public static readonly DependencyProperty ButtonDownCommandProperty =
        DependencyProperty.Register(nameof(ButtonDownCommand), typeof(ICommand), typeof(MouseLeftButtonDownBehavior));

    public ICommand ButtonDownCommand
    {
        get => (ICommand)GetValue(ButtonDownCommandProperty);
        set => SetValue(ButtonDownCommandProperty, value);
    } 

    protected override void OnAttached()
    {
        AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
    }
    protected override void OnCleanup()
    {
        AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
    }
    
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ButtonDownCommand?.CanExecute(AssociatedObject.DataContext) != true) return;
        
        ButtonDownCommand.Execute(AssociatedObject.DataContext);
    }
}