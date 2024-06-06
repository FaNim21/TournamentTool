using System.Collections.ObjectModel;
using System.Configuration;
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

public class PlayerManagerViewModel : BaseViewModel
{
    public MainViewModel MainViewModel { get; set; }

    public ObservableCollection<PaceManEvent> PaceManEvents { get; set; } = [];

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

    private PaceManEvent? _choosenEvent;
    public PaceManEvent? ChoosenEvent
    {
        get { return _choosenEvent; }
        set
        {
            _choosenEvent = value;
            OnPropertyChanged(nameof(ChoosenEvent));
        }
    }

    public ICommand SavePlayerCommand { get; set; }

    public ICommand EditPlayerCommand { get; set; }
    public ICommand RemovePlayerCommand { get; set; }

    public ICommand LoadFromPaceManCommand { get; set; }
    public ICommand LoadFromCSVCommand { get; set; }

    public ICommand RemoveAllPlayerCommand { get; set; }
    public ICommand FixPlayersHeadsCommand { get; set; }

    public ICommand GoBackCommand { get; set; }


    public PlayerManagerViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
        GoBackCommand = new RelayCommand(GoBack);

        SavePlayerCommand = new RelayCommand(SavePlayer);

        EditPlayerCommand = new EditPlayerCommand(this);
        RemovePlayerCommand = new RemovePlayerCommand(this);

        LoadFromPaceManCommand = new RelayCommand(LoadDataFromPaceMan);
        LoadFromCSVCommand = new GetCSVPlayersDataCommand(this);

        RemoveAllPlayerCommand = new RelayCommand(RemoveAllPlayers);
        FixPlayersHeadsCommand = new RelayCommand(FixPlayersHeads);
    }

    public override void OnEnable(object? parameter)
    {
        if (parameter != null && parameter is Tournament tournament)
        {
            Tournament = tournament;
        }

        Player = new();

        Task.Run(async () =>
        {
            string result = await Helper.MakeRequestAsString("https://paceman.gg/api/cs/eventlist");
            PaceManEvent[]? eventsData = JsonSerializer.Deserialize<PaceManEvent[]>(result);
            if (eventsData == null) return;

            PaceManEvents = new(eventsData);
            OnPropertyChanged(nameof(PaceManEvents));
        });
    }
    public override bool OnDisable()
    {
        if (IsEditing)
        {
            DialogBox.Show("Finish editing before closing the window", "Editing");
            return false;
        }

        MainViewModel.SavePreset();
        return true;
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
                    current.TwitchName = Player.TwitchName.ToLower().Trim();
                    current.PersonalBest = Player.PersonalBest;
                    Task.Run(current.UpdateHeadImage);
                }
            }

            IsEditing = false;
        }
        else
        {
            Player newPlayer = new()
            {
                Name = Player.Name,
                InGameName = Player.InGameName,
                TwitchName = Player.TwitchName.ToLower().Trim(),
                PersonalBest = Player.PersonalBest,
            };
            Task.Run(newPlayer.UpdateHeadImage);
            Tournament!.AddPlayer(newPlayer);
        }
        Player = new();
        MainViewModel.SavePreset();
    }

    private void LoadDataFromPaceMan()
    {
        if (ChoosenEvent == null) return;

        Task.Run(LoadDataFromPaceManAsync);
    }
    private async Task LoadDataFromPaceManAsync()
    {
        using HttpClient client = new();

        var requestData = new { uuids = ChoosenEvent!.WhiteList };
        string jsonContent = JsonSerializer.Serialize(requestData);

        List<Player> eventPlayers = [];
        for (int i = 0; i < ChoosenEvent!.WhiteList!.Length; i++)
        {
            var current = ChoosenEvent!.WhiteList[i];
            Player player = new() { UUID = current };
            eventPlayers.Add(player);
        }

        HttpContent content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync("https://paceman.gg/api/us/twitch", content);

        if (!response.IsSuccessStatusCode) return;
        string responseContent = await response.Content.ReadAsStringAsync();
        List<PaceManTwitchResponse>? twitchNames = JsonSerializer.Deserialize<List<PaceManTwitchResponse>>(responseContent)!.Where(x => !string.IsNullOrEmpty(x.liveAccount)).ToList();
        if (twitchNames == null) return;

        for (int i = 0; i < twitchNames.Count; i++)
        {
            var current = twitchNames[i];
            if (Tournament!.IsNameDuplicate(current.liveAccount))
            {
                twitchNames.RemoveAt(i);
                i--;
            }
        }

        for (int i = 0; i < eventPlayers.Count; i++)
        {
            var player = eventPlayers[i];
            for (int j = 0; j < twitchNames.Count; j++)
            {
                var twitch = twitchNames[j];
                if (player.UUID == twitch.uuid)
                {
                    player.TwitchName = twitch.liveAccount;
                    player.Name = twitch.liveAccount;
                    player.PersonalBest = "Unk";
                    await player.CompleteData();
                    Tournament!.AddPlayer(player);
                    break;
                }
            }
        }

        DialogBox.Show("Done loading data from paceman event");
    }

    private void RemoveAllPlayers()
    {
        MessageBoxResult result = DialogBox.Show("Are you sure you want to remove all players from whitelist?", "Removing players", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
            Tournament!.Players.Clear();
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
        });
    }

    public void GoBack()
    {
        MainViewModel.Open<PresetManagerViewModel>();
    }
}
