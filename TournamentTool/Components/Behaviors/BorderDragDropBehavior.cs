using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TournamentTool.Components.Behaviors;

public class BorderDragDropBehavior : BehaviorBase<Border>
{
    public static readonly DependencyProperty OnCommandProperty = DependencyProperty.Register(nameof(OnCommand), typeof(ICommand), typeof(BorderDragDropBehavior));
    public static readonly DependencyProperty DragDataTypeProperty = DependencyProperty.Register(nameof(DragDataType), typeof(Type), typeof(BorderDragDropBehavior));

    public ICommand OnCommand
    {
        get => (ICommand)GetValue(OnCommandProperty);
        set => SetValue(OnCommandProperty, value);
    }
    public Type DragDataType
    {
        get => (Type)GetValue(DragDataTypeProperty);
        set => SetValue(DragDataTypeProperty, value);
    }


    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseMove += OnMouseMove;
        //AssociatedObject.MouseEnter += MouseEnter;
    }

    protected override void OnCleanup()
    {
        base.OnCleanup();
        AssociatedObject.MouseMove -= OnMouseMove;
        //AssociatedObject.MouseEnter -= MouseEnter;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        if (OnCommand != null && OnCommand.CanExecute(null))
        {
            OnCommand.Execute(null);
        }
        var datatype = DragDataType ?? typeof(object);

        DragDrop.DoDragDrop(AssociatedObject, new DataObject(datatype, AssociatedObject.DataContext), DragDropEffects.Move);
    }

    /*private void MouseEnter(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        if (OnCommand != null && OnCommand.CanExecute(null))
        {
            OnCommand.Execute(null);
        }
        var datatype = DragDataType ?? typeof(object);

        DragDrop.DoDragDrop(AssociatedObject, new DataObject(datatype, AssociatedObject.DataContext), DragDropEffects.Move);
    }*/
}
