using System.Net.Http;
using System.Text.Json;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Extensions;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules.Logging;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.Commands.PlayerManager;

public class LoadDataFromPacemanCommand : BaseCommand
{
    private PlayerManagerViewModel PlayerManager { get; }
    private ILoggingService Logger { get; }

    private readonly ITournamentManager _tournamentManager;
    private readonly IPresetSaver _presetSaver;
    private readonly ILoadingDialog _loadingDialog;

    private List<PaceManTwitchResponse>? _twitchNames;
    private List<PlayerViewModel> eventPlayers = [];

    private PaceManEvent? _chosenEvent;

    public LoadDataFromPacemanCommand(PlayerManagerViewModel playerManager, ITournamentManager tournamentManager, IPresetSaver presetSaver, ILoadingDialog loadingDialog, ILoggingService logger)
    {
        PlayerManager = playerManager;
        Logger = logger;
        _tournamentManager = tournamentManager;
        _presetSaver = presetSaver;
        _loadingDialog = loadingDialog;
    }

    public override void Execute(object? parameter)
    {
        _chosenEvent = PlayerManager.ChosenEvent;
        if (_chosenEvent == null || string.IsNullOrEmpty(_chosenEvent.ID)) return;
        _loadingDialog.ShowLoading(LoadDataFromPaceManAsync);
    }

    private async Task LoadDataFromPaceManAsync(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        using HttpClient client = new();

        var requestData = new { uuids = _chosenEvent!.WhiteList };
        string jsonContent = JsonSerializer.Serialize(requestData);

        eventPlayers.Clear();
        logProgress.Report("Setting up players from paceman");
        for (int i = 0; i < _chosenEvent!.WhiteList!.Length; i++)
        {
            var current = _chosenEvent!.WhiteList[i];
            PlayerViewModel playerViewModel = PlayerManager.PlayerViewModelFactory.Create();
            playerViewModel.UUID = current.Replace("-", "") ?? string.Empty;
            eventPlayers.Add(playerViewModel);
        }

        _twitchNames?.Clear();
        _twitchNames = null;
        try
        {
            logProgress.Report("Getting twitch names from paceman api from the uuids");
            HttpContent content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Consts.PaceManTwitchAPI, content, cancellationToken);

            if (!response.IsSuccessStatusCode) return;
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _twitchNames = JsonSerializer.Deserialize<List<PaceManTwitchResponse>>(responseContent);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error while loading data from paceman: {ex}");
        }

        if (_twitchNames == null) return;
        cancellationToken.ThrowIfCancellationRequested();
        await UpdateWhitelist(progress, logProgress, cancellationToken);

        logProgress.Report("Loading complete");
        progress.Report(1);
        _presetSaver.SavePreset();
        Logger.Information($"Done loading data from paceman event: {_chosenEvent.Name}");
    }

    private async Task UpdateWhitelist(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        logProgress.Report("Removes duplicates from event with whitelist (stream data names)");
        for (int i = 0; i < _twitchNames!.Count; i++)
        {
            var current = _twitchNames[i];
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var player in _tournamentManager.Players)
            {
                if (!player.StreamData.ExistName(current.liveAccount)) continue;
                
                _twitchNames.RemoveAt(i);
                i--;
            }
        }
        
        logProgress.Report($"Adding non-duplicated players to whitelist");
        for (int i = 0; i < eventPlayers.Count; i++)
        {
            var player = eventPlayers[i];
            cancellationToken.ThrowIfCancellationRequested();
            for (int j = 0; j < _twitchNames.Count; j++)
            {
                var twitch = _twitchNames[j];
                progress.Report((float)i / eventPlayers.Count);
                if (player.UUID != twitch.uuid.Replace("-", "")) continue;
                player.StreamData.Main = twitch.liveAccount?.Trim() ?? string.Empty;
                player.PersonalBest = string.Empty;
                
                await player.CompleteData();
                if (_tournamentManager.ContainsDuplicatesNoDialog(player.Data)) continue;
                logProgress.Report($"({i+1}/{_twitchNames.Count}) Completed data from Paceman for player: {player.InGameName}");
                player.Name = twitch.liveAccount?.Trim() ?? player.InGameName;
                Application.Current.Dispatcher.Invoke(() => { PlayerManager.Add(player); });
                break;
            }
        }
    }
}
