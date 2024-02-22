using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.PlayerManager;
using TournamentTool.Models;

namespace TournamentTool.ViewModels;

public class PlayerManagerViewModel : BaseViewModel
{
    public MainViewModel MainViewModel { get; set; }

    private Tournament? _tournament;
    public Tournament? Tournament
    {
        get => _tournament; set
        {
            _tournament = value;
            OnPropertyChanged(nameof(Tournament));
        }
    }

    private Player? _player;
    public Player? Player
    {
        get => _player;
        set
        {
            _player = value;
            OnPropertyChanged(nameof(Player));
        }
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            _isEditing = value;
            OnPropertyChanged(nameof(IsEditing));
        }
    }

    public ICommand SavePlayerCommand { get; set; }

    public ICommand EditPlayerCommand { get; set; }
    public ICommand RemovePlayerCommand { get; set; }


    public PlayerManagerViewModel(MainViewModel mainViewModel, Tournament tournament)
    {
        MainViewModel = mainViewModel;
        Tournament = tournament;
        Player = new();

        foreach (var player in MainViewModel.CurrentChosen!.Players)
            player.Update();

        SavePlayerCommand = new RelayCommand(SavePlayer);

        EditPlayerCommand = new EditPlayerCommand(this);
        RemovePlayerCommand = new RemovePlayerCommand(this);
    }

    private void SavePlayer()
    {
        if (Player == null) return;
        if (string.IsNullOrEmpty(Player.Name) || string.IsNullOrEmpty(Player.TwitchName)) return;

        if (IsEditing)
        {
            for (int i = 0; i < Tournament!.Players.Count; i++)
            {
                var current = Tournament!.Players[i];
                if (current.Id.Equals(Player.Id))
                {
                    current.Name = Player.Name;
                    current.InGameName = Player.InGameName;
                    current.TwitchName = Player.TwitchName;
                    current.PersonalBest = Player.PersonalBest;
                    current.Update();
                }
            }

            IsEditing = false;
        }
        else
        {
            Tournament?.Players.Add(Player!);
        }
        Player = new();
        MainViewModel.SavePresetCommand.Execute(null);
    }
}
