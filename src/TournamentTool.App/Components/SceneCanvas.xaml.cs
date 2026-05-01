using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Core.Common.OBS;
using TournamentTool.Domain.Entities;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.App.Components;

public partial class SceneCanvas : UserControl
{
    //TODO: 0 TO CALE GOWNO DAC DO BEHAVIORS i rozbic to na mniejsze behaviors i przeniesc logike do viewmodeli
    //To jest okropne, bo chcialbym tutaj zrobic wchodzenie w interakcje z itemami poprzez klikanie w scene, ale 

    public SceneCanvas()
    {
        InitializeComponent();
    }

    private void PointOfView_Drop(object sender, DragEventArgs e)
    {
        if (sender is not Border droppedBorder) return;
        if (DataContext is not SceneViewModel scene) return;
        if (droppedBorder.DataContext is not PointOfViewViewModel pov) return;

        if (e.Data.GetData(typeof(IPlayer)) is IPlayer info)
        {
            if (!scene.ExistInItems<PointOfViewViewModel>(p => p.StreamDisplayInfo.Equals(info.StreamDisplayInfo)))
            {
                pov.SetPOV(info);
            }
            else
            {
                ISwappable<PointOfViewViewModel>? foundPov =
                    scene.GetItem<PointOfViewViewModel>(p => p.StreamDisplayInfo.Equals(info.StreamDisplayInfo));
                if (foundPov == null) return;

                foundPov.Swap(pov);
            }
        }
        else if (e.Data.GetData(typeof(PointOfViewViewModel)) is ISwappable<PointOfViewViewModel> dragPov)
        {
            dragPov.Swap(pov);
        }

        scene.Interactable?.UnSelectItems(true);
    }

    private void PointOfView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not SceneViewModel scene) return;
        if (scene.Interactable is null) return;

        if (sender is not Border clickedBorder) return;
        if (clickedBorder!.DataContext is not PointOfViewViewModel pov) return;

        scene.Interactable.OnPOVClick(scene, pov);
    }

    private void Border_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;

        if (sender is not Border border) return;
        if (border.DataContext is not PointOfViewViewModel pov) return;

        e.Handled = true;
        pov.Clear(true);
    }

    private void PointOfView_MouseEnter(object sender, MouseEventArgs e) 
        => Mouse.OverrideCursor = Cursors.Hand;
    private void PointOfView_MouseLeave(object sender, MouseEventArgs e) 
        => Mouse.OverrideCursor = null;
}
