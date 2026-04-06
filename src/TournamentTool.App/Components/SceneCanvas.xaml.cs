using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Domain.Entities;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Obs.Items;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.App.Components;

public partial class SceneCanvas : UserControl
{
    //TODO: 1 TO CALE GOWNO DAC DO BEHAVIORS

    public SceneCanvas()
    {
        InitializeComponent();
    }

    private async void PointOfView_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (sender is not Border droppedBorder) return;
            if (DataContext is not SceneViewModel scene) return;
            if (droppedBorder.DataContext is not PointOfViewViewModel pov) return;

            if (e.Data.GetData(typeof(IPlayer)) is IPlayer info)
            {
                if (!scene.ExistInItems<PointOfViewViewModel>(p => p.StreamDisplayInfo.Equals(info.StreamDisplayInfo)))
                {
                    await pov.SetPOVAsync(info);
                }
                else
                {
                    PointOfViewViewModel? foundPov =
                        scene.GetItem<PointOfViewViewModel>(p => p.StreamDisplayInfo.Equals(info.StreamDisplayInfo));
                    if (foundPov == null) return;

                    await foundPov.SwapAsync(pov);
                }
            }
            else if (e.Data.GetData(typeof(PointOfViewViewModel)) is PointOfViewViewModel dragPov)
            {
                await dragPov.SwapAsync(pov);
            }

            scene.Interactable.UnSelectItems(true);
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex);
        }
    }

    private async void PointOfView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (DataContext is not SceneViewModel scene) return;
            if (sender is not Border clickedBorder) return;
            if (clickedBorder!.DataContext is not PointOfViewViewModel pov) return;

            await scene.Interactable.OnPOVClickAsync(scene, pov);
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex);
        }
    }

    private async void Border_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        try
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;

            if (sender is not Border border) return;
            if (border.DataContext is not PointOfViewViewModel pov) return;

            e.Handled = true;
            await pov.ClearAsync(true);
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex);
        }
    }

    private void PointOfView_MouseEnter(object sender, MouseEventArgs e) 
        => Mouse.OverrideCursor = Cursors.Hand;
    private void PointOfView_MouseLeave(object sender, MouseEventArgs e) 
        => Mouse.OverrideCursor = null;
}
