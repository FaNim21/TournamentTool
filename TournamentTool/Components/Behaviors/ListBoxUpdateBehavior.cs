using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Modules.SidePanels;
using TournamentTool.ViewModels;

namespace TournamentTool.Components.Behaviors;

public class ListBoxUpdateBehavior : Behavior<ListBox>
{
    private static readonly List<ListBox> attachedListBoxes = [];


    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.SelectionChanged += OnSelectionChanged;
        attachedListBoxes.Add(AssociatedObject);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        attachedListBoxes.Remove(AssociatedObject);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AssociatedObject == null) return;
        UpdateAllListBoxesLayouts();

        ControllerViewModel? controller = null;
        BaseViewModel viewModel = (BaseViewModel)AssociatedObject.DataContext;
        if (viewModel is SidePanel sidePanel)
            controller = sidePanel.Controller;

        if (viewModel is ControllerViewModel controllerViewModel)
            controller = controllerViewModel; 

        if (controller == null) return;
        if (controller.CurrentChosenPOV != null)
        {
            UnselectAllListBoxes();
            Keyboard.ClearFocus();
            controller.CurrentChosenPOV = null;
        }
    }

    private void UpdateAllListBoxesLayouts()
    {
        for (int i = 0; i < attachedListBoxes.Count; i++)
        {
            var listBox = attachedListBoxes[i];
            listBox.UpdateLayout();
        }
    }

    private void UnselectAllListBoxes()
    {
        for (int i = 0; i < attachedListBoxes.Count; i++)
        {
            var listBox = attachedListBoxes[i];
            listBox.UnselectAll();
        }
    }
}
