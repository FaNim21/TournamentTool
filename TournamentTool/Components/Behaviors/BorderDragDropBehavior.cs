using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Models;

namespace TournamentTool.Components.Behaviors;

public class BorderDragDropBehavior : BehaviorBase<Border>
{
    public static readonly DependencyProperty OnCommandProperty =
        DependencyProperty.Register(nameof(OnCommand), typeof(ICommand), typeof(BorderDragDropBehavior));


    public ICommand OnCommand
    {
        get => (ICommand)GetValue(OnCommandProperty);
        set => SetValue(OnCommandProperty, value);
    }

    private static int bilans;


    protected override void OnAttached()
    {
        base.OnAttached();
        bilans++;
        Trace.WriteLine($"Attached dragdrop with bilans: {bilans}");
        AssociatedObject.MouseDown += OnMouseDown;
    }

    protected override void OnCleanup()
    {
        base.OnCleanup();
        bilans--;
        Trace.WriteLine($"Cleanup dragdrop with bilans: {bilans}");
        AssociatedObject.MouseDown -= OnMouseDown;
    }

    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Border border) return;

        if(OnCommand != null && OnCommand.CanExecute(null))
        {
            OnCommand.Execute(null);
        }

        DragDrop.DoDragDrop(border, new DataObject(typeof(IPlayer), border.DataContext), DragDropEffects.Move);
    }
}
