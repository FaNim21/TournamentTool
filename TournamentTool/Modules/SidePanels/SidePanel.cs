using System.Windows.Input;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.SidePanels;

public abstract class SidePanel : BaseViewModel, IPovDragAndDropContext
{
    private ControllerViewModel Controller { get; init; }
    private LeaderboardPanelViewModel Leaderboard { get; init; }

    protected TournamentViewModel TournamentViewModel { get; init; }

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


    protected SidePanel(ControllerViewModel controller, TournamentViewModel tournamentViewModel, LeaderboardPanelViewModel leaderboard)
    {
        UnselectItemsCommand = controller.UnSelectItemsCommand;

        Controller = controller;
        TournamentViewModel = tournamentViewModel;
        Leaderboard = leaderboard;
        
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
