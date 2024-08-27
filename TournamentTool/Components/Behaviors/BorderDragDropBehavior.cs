using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Models;

namespace TournamentTool.Components.Behaviors;

public class BorderDragDropBehavior : Behavior<Border>
{
    public static readonly DependencyProperty OnCommandProperty =
        DependencyProperty.Register(nameof(OnCommand), typeof(ICommand), typeof(BorderDragDropBehavior));

    public ICommand OnCommand
    {
        get => (ICommand)GetValue(OnCommandProperty);
        set => SetValue(OnCommandProperty, value);
    }


    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseDown += OnMouseDown;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.MouseDown -= OnMouseDown;
    }

    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Border border) return;

        if(OnCommand != null && OnCommand.CanExecute(null))
        {
            //OnCommand.Execute(null);
        }

        DragDrop.DoDragDrop(border, new DataObject(typeof(IPlayer), border.DataContext), DragDropEffects.Move);
    }
}
