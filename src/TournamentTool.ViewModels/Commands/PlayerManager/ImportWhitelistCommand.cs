using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic.FileIO;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels.Commands.PlayerManager;

public class ImportWhitelistCommand : BaseCommand
{
    private struct JSONPlayer
    {
        [JsonPropertyName("ign")]
        public string IGN { get; set; }

        [JsonPropertyName("twitch_username")]
        public string Twitch { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
    }

    private PlayerManagerViewModel PlayerManager { get; }
    public ILoggingService Logger { get; }
    private IDispatcherService Dispatcher { get; }

    private readonly ITournamentManager _tournamentManager;
    private readonly IPresetSaver _presetSaver;
    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;
    private string _path = string.Empty;
    private string _fileName = string.Empty;

    
    public ImportWhitelistCommand(PlayerManagerViewModel playerManager, ITournamentManager tournamentManager, IPresetSaver presetSaver, ILoggingService logger, 
        IWindowService windowService, IDispatcherService dispatcher, IDialogService dialogService)
    {
        PlayerManager = playerManager;
        Logger = logger;
        Dispatcher = dispatcher;

        _tournamentManager = tournamentManager;
        _presetSaver = presetSaver;
        _windowService = windowService;
        _dialogService = dialogService;
    }
    
    public override void Execute(object? parameter)
    {
        _path = _dialogService.ShowOpenFile("All Files (*.json, *.ranked, *.csv)|*.json;*.ranked;*.csv");
        
        if (string.IsNullOrEmpty(_path)) return;
        _fileName = Path.GetFileName(_path);
        
        string extension = Path.GetExtension(_path).ToLower();
        switch (extension)
        {
            case ".json":
                _windowService.ShowLoading(ImportMain);
                break;
            case ".ranked":
                _windowService.ShowLoading(ImportRanked);
                break;
            case ".csv":
                _windowService.ShowLoading(ImportCSV);
                break;
        }
        _presetSaver.SavePreset();
    }

    private async Task ImportMain(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        logProgress.Report("Reading file data");
        string text = await File.ReadAllTextAsync(_path, cancellationToken);
        if (string.IsNullOrEmpty(text)) return;
        
        List<Player>? players = null;
        try
        {
            players = JsonSerializer.Deserialize<List<Player>>(text);
        }
        catch
        {
            Logger.Error("Error loading player from whitelist");
        }

        if (players == null) return;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            IPlayerViewModel createdPlayer = PlayerManager.PlayerViewModelFactory.Create();
            if (createdPlayer is not PlayerViewModel viewModel) continue;
            
            progress.Report((float)i/players.Count);
            logProgress.Report($"({i+1}/{players.Count}) Checking for duplicates for player: {player.InGameName}");
            if (_tournamentManager.ContainsDuplicatesNoDialog(player)) continue;
            logProgress.Report($"({i+1}/{players.Count}) Adding player: {player.InGameName}");

            viewModel.UpdateHeadBitmap();
            await Dispatcher.InvokeAsync(() => { PlayerManager.Add(viewModel); });
        }
        
        logProgress.Report("Loading complete");
        progress.Report(1);
        Logger.Information($"Done loading data from {_fileName} whitelist file");
    }
    private async Task ImportCSV(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        int totalLines = File.ReadLines(_path).Count();
                
        using TextFieldParser parser = new(_path);
        parser.HasFieldsEnclosedInQuotes = true;
        parser.SetDelimiters(",");
        parser.ReadLine();
        
        float count = 0;
        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();
                    
            progress.Report(count / totalLines);
                    
            string[]? fields = parser.ReadFields();
            if (fields == null) continue;

            IPlayerViewModel createdPlayer = PlayerManager.PlayerViewModelFactory.Create();
            if (createdPlayer is not PlayerViewModel player) continue;
            
            player.Name = fields[0];
            player.InGameName = fields[1].Trim();
            player.UUID = fields[2].Replace("-", "");
            player.PersonalBest = fields[3];
            if (fields.Length > 4) 
            {
                player.StreamData.SetName(fields[4].ToLower().Trim());
                if (fields.Length > 5)
                    player.StreamData.SetName(fields[5].ToLower().Trim());
            }

            if (_tournamentManager.ContainsDuplicatesNoDialog(player.Data))
            {
                totalLines--;
                continue;
            }
                    
            logProgress.Report($"({count+1}/{totalLines}) Completing data for {player.InGameName}");
            await player.CompleteData();
            await Dispatcher.InvokeAsync(() => { PlayerManager.Add(player); });
            count++;
        }
        
        logProgress.Report("Loading complete");
        progress.Report(1);
        Logger.Information($"Done loading data from {_fileName} file");
    }
    private async Task ImportRanked(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        try
        {
            string json = await File.ReadAllTextAsync(_path, cancellationToken);
            JSONPlayer[] loadedPlayers = JsonSerializer.Deserialize<JSONPlayer[]>(json)!;
            if (loadedPlayers.Length == 0) return;
     
            int length = loadedPlayers.Length;
            List<PlayerViewModel> playersToComplete = [];
            
            for (int i = 0; i < length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report((float)i / length);
                     
                var loadedPlayer = loadedPlayers[i];
     
                IPlayerViewModel createdPlayer = PlayerManager.PlayerViewModelFactory.Create();
                if (createdPlayer is not PlayerViewModel player) continue;
                
                player.Name = loadedPlayer.DisplayName;
                player.InGameName = loadedPlayer.IGN.Trim();
                player.StreamData.SetName(loadedPlayer.Twitch.Trim());
                if (_tournamentManager.ContainsDuplicatesNoDialog(player.Data)) continue;
                
                logProgress.Report($"({i+1}/{length}) Completing data for {player.InGameName}");
                await player.CompleteData(false);

                if (string.IsNullOrWhiteSpace(player.UUID))
                {
                    player.IsUUIDEmpty = true;
                    playersToComplete.Add(player);
                }
                await Dispatcher.InvokeAsync(() => { PlayerManager.Add(player); });
            }
            
            if (playersToComplete.Count > 0)
            {
                await FetchAndAssignUUIDs(playersToComplete, logProgress, cancellationToken);
            }
     
            logProgress.Report("Loading complete");
            progress.Report(1);
            Logger.Information($"Done loading data from {_fileName} ranked file");
        }
        catch (OperationCanceledException){}
        catch (Exception ex)
        {
            Logger.Error($"Error loading json data: {ex}");
        }   
    }
    
    private async Task FetchAndAssignUUIDs(List<PlayerViewModel> players, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        for (int i = 0; i < players.Count; i += 10)
        {
            var batch = players.Skip(i).Take(10).ToList();
            string[] names = batch.Select(p => p.InGameName!).ToArray();

            logProgress.Report($"Fetching UUIDs for {i}-{i + batch.Count} players");

            try
            {
                using HttpClient client = new();
                var jsonRequest = JsonSerializer.Serialize(names);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("https://api.mojang.com/profiles/minecraft", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logProgress.Report($"Error fetching UUIDs: {response.StatusCode}");
                    continue;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var uuidResults = JsonSerializer.Deserialize<List<ResponseMojangProfileAPI>>(jsonResponse);
                if (uuidResults == null) return;
                
                foreach (var result in uuidResults)
                {
                    var player = batch.FirstOrDefault(p => p.InGameName!.Equals(result.InGameName, StringComparison.OrdinalIgnoreCase));
                    if (player == null) continue;
                    
                    player.UUID = result.UUID;
                    logProgress.Report($"Assigned UUID {player.UUID} to {player.InGameName}");
                }
            }
            catch (Exception ex)
            {
                logProgress.Report($"Error fetching UUIDs: {ex.Message}");
            }
        }
    }

}