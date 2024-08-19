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
    public ICommand LoadFromJSONCommand { get; set; }

    public ICommand RemoveAllPlayerCommand { get; set; }
    public ICommand FixPlayersHeadsCommand { get; set; }

    public ICommand GoBackCommand { get; set; }


    public PlayerManagerViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        CanBeDestroyed = true;

        SavePlayerCommand = new RelayCommand(async ()=> { await SavePlayer(); });

        EditPlayerCommand = new EditPlayerCommand(this);
        RemovePlayerCommand = new RemovePlayerCommand(this);

        LoadFromPaceManCommand = new RelayCommand(async () => { await LoadDataFromPaceManAsync(); });
        LoadFromCSVCommand = new GetCSVPlayersDataCommand(this);
        LoadFromJSONCommand = new GetJSONPlayersDataCommand(this);

        RemoveAllPlayerCommand = new RelayCommand(RemoveAllPlayers);
        FixPlayersHeadsCommand = new RelayCommand(FixPlayersHeads);

        GoBackCommand = new RelayCommand(GoBack);
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

        Player = null;
        ChoosenEvent = null;

        PaceManEvents.Clear();
        return true;
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

    private async Task LoadDataFromPaceManAsync()
    {
        if (ChoosenEvent == null) return;

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
        HttpResponseMessage response = await client.PostAsync(Consts.PaceManTwitchAPI, content);

        if (!response.IsSuccessStatusCode) return;
        string responseContent = await response.Content.ReadAsStringAsync();
        List<PaceManTwitchResponse>? twitchNames = JsonSerializer.Deserialize<List<PaceManTwitchResponse>>(responseContent);
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
                    player.StreamData.Main = twitch.liveAccount ?? string.Empty;
                    player.PersonalBest = "Unk";
                    await player.CompleteData();
                    player.Name = twitch.liveAccount ?? player.InGameName;
                    Tournament!.AddPlayer(player);
                    break;
                }
            }
        }

        SavePreset();
        DialogBox.Show("Done loading data from paceman event");
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

    public void GoBack()
    {
        MainViewModel.Open<PresetManagerViewModel>();
    }
}
