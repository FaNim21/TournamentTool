using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.PlayerManager;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels;

public class PlayerManagerViewModel : SelectableViewModel
{
    public ObservableCollection<PaceManEvent> PaceManEvents { get; set; } = [];

    private Tournament _tournament = new();
    public Tournament Tournament
    {
        get => _tournament;
        set
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
            SaveButtonText = value ? "Save" : "Add";
            OnPropertyChanged(nameof(IsEditing));
        }
    }

    private string _saveButtonText = "Add";
    public string SaveButtonText
    {
        get => _saveButtonText;
        set
        {
            _saveButtonText = value;
            OnPropertyChanged(nameof(SaveButtonText));
        }
    }

    private PaceManEvent? _chosenEvent;
    public PaceManEvent? ChosenEvent
    {
        get { return _chosenEvent; }
        set
        {
            _chosenEvent = value;
            OnPropertyChanged(nameof(ChosenEvent));
        }
    }

    public ICommand SavePlayerCommand { get; set; }

    public ICommand EditPlayerCommand { get; set; }
    public ICommand RemovePlayerCommand { get; set; }

    public ICommand ImportPlayersCommand { get; set; }
    public ICommand ExportPlayersCommand { get; set; }

    public ICommand LoadFromPaceManCommand { get; set; }
    public ICommand LoadFromCSVCommand { get; set; }
    public ICommand LoadFromJSONCommand { get; set; }

    public ICommand RemoveAllPlayerCommand { get; set; }
    public ICommand FixPlayersHeadsCommand { get; set; }


    public PlayerManagerViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        SavePlayerCommand = new RelayCommand(async ()=> { await SavePlayer(); });

        EditPlayerCommand = new EditPlayerCommand(this);
        RemovePlayerCommand = new RemovePlayerCommand(this);

        ImportPlayersCommand = new RelayCommand(ImportPlayers);
        ExportPlayersCommand = new RelayCommand(ExportPlayers);

        LoadFromPaceManCommand = new LoadDataFromPacemanCommand(this);
        LoadFromCSVCommand = new GetCSVPlayersDataCommand(this);
        LoadFromJSONCommand = new GetJSONPlayersDataCommand(this);

        RemoveAllPlayerCommand = new RelayCommand(RemoveAllPlayers);
        FixPlayersHeadsCommand = new RelayCommand(FixPlayersHeads);

        Task.Run(async () =>
        {
            PaceManEvent[]? eventsData = null;
            try
            {
                string result = await Helper.MakeRequestAsString("https://paceman.gg/api/cs/eventlist");
                eventsData = JsonSerializer.Deserialize<PaceManEvent[]>(result);
            }
            catch { }

            if (eventsData == null) return;

            PaceManEvents = new(eventsData);
            OnPropertyChanged(nameof(PaceManEvents));
        });
    }

    public override bool CanEnable(Tournament tournament)
    {
        if (tournament is null) return false;

        Tournament = tournament;
        return true;
    }
    public override void OnEnable(object? parameter)
    {
        Player = new();
    }
    public override bool OnDisable()
    {
        if (IsEditing)
        {
            DialogBox.Show("Finish editing before closing the window", "Editing");
            return false;
        }

        Player = null;
        ChosenEvent = null;

        return true;
    }

    private void ImportPlayers()
    {
        OpenFileDialog openFileDialog = new() { Filter = "All Files (*.json)|*.json", };
        string path = openFileDialog.ShowDialog() == true ? openFileDialog.FileName : string.Empty;
        if (string.IsNullOrEmpty(path)) return;
    }

    public void ExportPlayers()
    {

    }

    private async Task SavePlayer()
    {
        if (Player == null) return;
        if (string.IsNullOrEmpty(Player.Name) || string.IsNullOrEmpty(Player.InGameName)) return;

        if (IsEditing)
        {
            for (int i = 0; i < Tournament!.Players.Count; i++)
            {
                var current = Tournament!.Players[i];
                if (current.Id.Equals(Player.Id))
                {
                    current.Name = Player.Name;
                    current.InGameName = Player.InGameName;
                    current.StreamData.Main = Player.StreamData.Main.ToLower().Trim();
                    current.StreamData.Alt = Player.StreamData.Alt.ToLower().Trim();
                    current.PersonalBest = Player.PersonalBest;
                    await current.UpdateHeadImage();
                }
            }

            IsEditing = false;
        }
        else
        {
            Player newPlayer = new() { Name = Player.Name, InGameName = Player.InGameName, PersonalBest = Player.PersonalBest };
            newPlayer.StreamData.Main = Player.StreamData.Main.ToLower().Trim();
            newPlayer.StreamData.Alt = Player.StreamData.Alt.ToLower().Trim();

            if (Tournament!.IsNameDuplicate(newPlayer.StreamData.Main))
            {
                DialogBox.Show($"{newPlayer.StreamData.Main} main twitch name exist in whitelist");
                return;
            }
            else if (Tournament.IsNameDuplicate(newPlayer.StreamData.Alt))
            {
                DialogBox.Show($"{newPlayer.StreamData.Alt} alt twitch name exist in whitelist");
                return;
            }

            await newPlayer.UpdateHeadImage();
            Tournament!.AddPlayer(newPlayer);
        }
        Player = new();
        SavePreset();
    }

    private void RemoveAllPlayers()
    {
        MessageBoxResult result = DialogBox.Show("Are you sure you want to remove all players from whitelist?", "Removing players", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
            Tournament!.Players.Clear();

        SavePreset();
    }

    private void FixPlayersHeads()
    {
        Task.Run(async () =>
        {
            for (int i = 0; i < Tournament!.Players.Count; i++)
            {
                var current = Tournament!.Players[i];
                await current.ForceUpdateHeadImage();
            }

            DialogBox.Show("Done fixing players head skins");
            SavePreset();
        });
    }

    public void SavePreset()
    {
        MainViewModel.SavePreset();
    }
}
