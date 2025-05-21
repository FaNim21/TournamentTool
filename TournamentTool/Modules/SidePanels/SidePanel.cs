using System.Windows.Input;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.SidePanels;

public abstract class SidePanel : BaseViewModel, IPovDragAndDropContext, IBackgroundDataReceiver
{
    private ControllerViewModel Controller { get; init; }

    private IPlayer? _selectedPlayer;
    public IPlayer? SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            Controller.ClearSelectedWhitelistPlayer();
            ClearSelectedPlayer();

            _selectedPlayer = value;
            OnPropertyChanged(nameof(SelectedPlayer));

            Controller.SetPovAfterClickedCanvas(value!);
        }
    }
    
    public PointOfView? CurrentChosenPOV
    {
        get => Controller.CurrentChosenPOV;
        set => Controller.CurrentChosenPOV = value;
    }

    public ControllerMode Mode { get; protected set; }

    public ICommand UnselectItemsCommand { get; private set; }


    protected SidePanel(ControllerViewModel controller)
    {
        UnselectItemsCommand = controller.UnSelectItemsCommand;
        Controller = controller;
        Mode = ControllerMode.None;
    }

    public override void OnEnable(object? parameter) { }
    public override bool OnDisable() { return true; }

    protected void RefreshControllerPlayers() => Controller.RefreshFilteredCollection();
    
    public void ClearSelectedPlayer()
    {
        _selectedPlayer = null;
        OnPropertyChanged(nameof(SelectedPlayer));
    }

}