using System.Diagnostics;
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
        Trace.WriteLine("Constructor: " + GetType().Name);
    }

    public override void OnEnable(object? parameter)
    {
        Trace.WriteLine("OnEnable: " + GetType().Name);
    }

    public override bool OnDisable()
    {
        Trace.WriteLine("OnDisable: " + GetType().Name);
        return true;
    }

    public void ClearSelectedPlayer()
    {
        _selectedPlayer = null;
        OnPropertyChanged(nameof(SelectedPlayer));
    }
}
