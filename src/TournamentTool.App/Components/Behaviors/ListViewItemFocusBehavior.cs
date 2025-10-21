using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TournamentTool.App.Components.Behaviors;

public class ListViewItemFocusBehavior : BehaviorBase<ListView>
{
    public static readonly DependencyProperty OnItemListClickProperty = DependencyProperty.Register(nameof(OnItemListClick), typeof(ICommand), typeof(ListViewItemFocusBehavior));
    public ICommand OnItemListClick
    {
        get => (ICommand)GetValue(OnItemListClickProperty);
        set => SetValue(OnItemListClickProperty, value);
    }
    
     
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += OnItemFocus;
        AssociatedObject.PreviewMouseRightButtonDown += OnItemFocus;
    }

    protected override void OnCleanup()
    {
        base.OnCleanup();
        AssociatedObject.PreviewMouseLeftButtonDown -= OnItemFocus;
        AssociatedObject.PreviewMouseRightButtonDown -= OnItemFocus;
    }
    
    private void OnItemFocus(object sender, MouseButtonEventArgs e)
    {
        ListViewItem? clickedItem = UIHelper.GetUIItemUnderMouse<ListViewItem>(AssociatedObject, e.GetPosition);
        if (clickedItem == null) return;

        Keyboard.Focus(clickedItem);
        OnItemListClick.Execute(null);
    }
}