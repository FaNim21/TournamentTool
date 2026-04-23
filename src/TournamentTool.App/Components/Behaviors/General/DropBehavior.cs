using System.Windows;
using System.Windows.Input;

namespace TournamentTool.App.Components.Behaviors.General;

public class DropBehavior : BehaviorBase<FrameworkElement>
{
    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.Register(nameof(DropCommand), typeof(ICommand), typeof(DropBehavior));

    public ICommand DropCommand
    {
        get => (ICommand)GetValue(DropCommandProperty);
        set => SetValue(DropCommandProperty, value);
    }

    protected override void OnAttached()
    {
        AssociatedObject.AllowDrop = true;
        AssociatedObject.Drop += OnDrop;
    }

    protected override void OnCleanup()
    {
        AssociatedObject.Drop -= OnDrop;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (DropCommand == null) return;

        //TODO: 2 cos z tym drop context zrobic zeby wysylac dane do komendy
        /*var parameter = new DropContext
        {
            Sender = sender,
            Data = e.Data,
            OriginalEventArgs = e
        };*/

        /*if (DropCommand.CanExecute(parameter))
            DropCommand.Execute(parameter);*/
    }
}