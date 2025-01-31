using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.PlayerManager;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.Windows;

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

    public ICommand AddPlayerCommand { get; set; }
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
        AddPlayerCommand = new RelayCommand(AddPlayer);
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
    public override void OnEnable(object? parameter) { }
    public override bool OnDisable()
    {
        ChosenEvent = null;

        return true;
    }

    private void AddPlayer()
    {
        AddPlayer(null);
    }
    public void AddPlayer(Player? player = null)
    {
        WhitelistPlayerWindowViewModel viewModel = new(this, player);
        WhitelistPlayerWindow window = new(viewModel) { Owner = Application.Current.MainWindow };

        MainViewModel.BlockWindow();
        window.ShowDialog();
        MainViewModel.UnBlockWindow();
    }

    private void ImportPlayers()
    {
        OpenFileDialog openFileDialog = new() { Filter = "All Files (*.json)|*.json", };
        string path = openFileDialog.ShowDialog() == true ? openFileDialog.FileName : string.Empty;
        if (string.IsNullOrEmpty(path)) return;
        //TODO: 0 zrobic import
    }
    private void ExportPlayers()
    {
        //TODO: 0 zrobic export
    }

    public async Task<bool> SavePlayer(Player player, bool isEditing)
    {
        if (string.IsNullOrEmpty(player.Name))
        {
            DialogBox.Show($"Name cannot be empty", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (string.IsNullOrEmpty(player.InGameName))
        {
            DialogBox.Show($"InGameName cannot be empty", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (isEditing)
        {
            if (!await EditPlayer(player)) return false;
        }
        else
        {
            if (!await AddPlayerToWhitelist(player)) return false;
        }

        SavePreset();
        return true;
    }
    private async Task<bool> EditPlayer(Player player)
    {
        for (int i = 0; i < Tournament!.Players.Count; i++)
        {
            var current = Tournament!.Players[i];
            if (current.Id.Equals(player.Id))
            {
                bool success = await UpdatePlayerData(current, player, player.Id);
                return success;
            }
        }
        return true;
    }
    private async Task<bool> AddPlayerToWhitelist(Player player)
    {
        Player newplayer = new() { };
        bool success = await UpdatePlayerData(newplayer, player);
        if (!success) return false;

        Tournament!.AddPlayer(newplayer);
        return true;
    }
    private async Task<bool> UpdatePlayerData(Player player, Player windowsData, Guid? excludeID = null)
    {
        if (Tournament.ContainsDuplicates(windowsData, excludeID)) return false;

        player.Name = windowsData.Name;
        player.InGameName = windowsData.InGameName;
        player.PersonalBest = windowsData.PersonalBest;
        player.StreamData.Main = windowsData.StreamData.Main.ToLower().Trim();
        player.StreamData.Alt = windowsData.StreamData.Alt.ToLower().Trim();

        await player.UpdateHeadImage();
        return true;
    }

    private void RemoveAllPlayers()
    {
        MessageBoxResult result = DialogBox.Show("Are you sure you want to remove all players from whitelist?", "Removing players", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

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
