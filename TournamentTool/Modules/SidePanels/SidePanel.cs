using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.SidePanels;

public abstract class SidePanel : BaseViewModel
{
    public ControllerViewModel Controller { get; set; }

    private IPlayer? _selectedPlayer;
    public IPlayer? SelectedPlayer
    {
        get { return _selectedPlayer; }
        set
        {
            Controller.ClearSelectedWhitelistPlayer();
            ClearSelectedPlayer();

            Controller.CurrentChosenPlayer = value;
            Controller.SetPovAfterClickedCanvas();
        }
    }


    public SidePanel(ControllerViewModel controller)
    {
        Controller = controller;
    }

    public void ClearSelectedPlayer()
    {
        _selectedPlayer = null;
        OnPropertyChanged(nameof(SelectedPlayer));
    }
}
