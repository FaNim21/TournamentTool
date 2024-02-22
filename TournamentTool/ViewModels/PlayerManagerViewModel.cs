using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TournamentTool.Commands;
using TournamentTool.Commands.PlayerManager;
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


    public PlayerManagerViewModel(MainViewModel mainViewModel, Tournament tournament)
    {
        MainViewModel = mainViewModel;
        Tournament = tournament;
        Player = new();

        SavePlayerCommand = new RelayCommand(SavePlayer);

        EditPlayerCommand = new EditPlayerCommand(this);
        RemovePlayerCommand = new RemovePlayerCommand(this);

        LoadFromPaceManCommand = new RelayCommand(LoadDataFromPaceMan);
        LoadFromCSVCommand = new GetCSVPlayersDataCommand(this);

        Task.Run(async () =>
        {
            string result = await Helper.MakeRequestAsString("https://paceman.gg/api/cs/eventlist");
            PaceManEvent[]? eventsData = JsonSerializer.Deserialize<PaceManEvent[]>(result);
            if (eventsData == null) return;

            PaceManEvents = new(eventsData);
            OnPropertyChanged(nameof(PaceManEvents));
        });
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
            Player newPlayer = new()
            {
                Name = Player.Name,
                InGameName = Player.InGameName,
                TwitchName = Player.TwitchName,
                PersonalBest = Player.PersonalBest,
            };
            newPlayer.Update();
            Tournament?.Players.Add(newPlayer);
        }
        Player = new();
        MainViewModel.SavePresetCommand.Execute(null);
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
                    Application.Current.Dispatcher.Invoke(() => { Tournament!.Players.Add(player); });
                    break;
                }
            }
        }
    }
}
