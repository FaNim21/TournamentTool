using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.SidePanels;

public abstract class SidePanel : BaseViewModel
{
    public ControllerViewModel Controller { get; private set; }
    public LeaderboardPanelViewModel Leaderboard { get; private set; }

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
        Leaderboard = controller.Leaderboard;
        Mode = ControllerMode.None;
    }

    public abstract void Initialize();
    public abstract void UnInitialize();
    
    public override void OnEnable(object? parameter) { }
    public override bool OnDisable() { return true; }

    public void UpdatePlayerStreamData(string inGameName, string twitchName)
    {
        int n = Controller.TournamentViewModel.Players.Count;
        bool updatedPlayer = false;
        
        for (int i = 0; i < n; i++)
        {
            var player = Controller.TournamentViewModel.Players[i];
            if (!player.InGameName!.Equals(inGameName, StringComparison.OrdinalIgnoreCase)) continue;
            
            updatedPlayer = true;
            player.StreamData.SetName(twitchName);
            Controller.FilterItems();
        }
        
        if (updatedPlayer)
        {
            Controller.SavePreset();
        }
    }
    
    public void EvaluatePlayerInLeaderboard(LeaderboardPlayerEvaluateData data)
    {
        Leaderboard.EvaluatePlayer(data);
    }
    
    public void ClearSelectedPlayer()
    {
        _selectedPlayer = null;
        OnPropertyChanged(nameof(SelectedPlayer));
    }
}
