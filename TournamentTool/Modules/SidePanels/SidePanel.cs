using TournamentTool.Enums;
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

            _selectedPlayer = value;
            OnPropertyChanged(nameof(SelectedPlayer));

            Controller.SetPovAfterClickedCanvas(value!);
        }
    }

    public ControllerMode Mode { get; protected set; }


    protected SidePanel(ControllerViewModel controller)
    {
        Controller = controller;
        Mode = ControllerMode.None;
    }

    public abstract void Initialize();
    public abstract void UnInitialize();
    
    public override void OnEnable(object? parameter) { }
    public override bool OnDisable() { return true; }

    public void ClearSelectedPlayer()
    {
        _selectedPlayer = null;
        OnPropertyChanged(nameof(SelectedPlayer));
    }
}
