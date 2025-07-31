using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using TournamentTool.Components.Controls;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.Commands.PlayerManager;

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

    private readonly ITournamentManager _tournamentManager;
    private readonly IPresetSaver _presetSaver;
    private readonly ILoadingDialog _loadingDialog;
    private string _path = string.Empty;

    
    public ImportWhitelistCommand(PlayerManagerViewModel playerManager, ITournamentManager tournamentManager, ILoadingDialog loadingDialog, IPresetSaver presetSaver)
    {
        PlayerManager = playerManager;
        
        _loadingDialog = loadingDialog;
        _tournamentManager = tournamentManager;
        _presetSaver = presetSaver;
    }
    
    public override void Execute(object? parameter)
    {
        OpenFileDialog openFileDialog = new() { Filter = "All Files (*.json, *.ranked, *.csv)|*.json;*.ranked;*.csv", };
        _path = openFileDialog.ShowDialog() == true ? openFileDialog.FileName : string.Empty;
        if (string.IsNullOrEmpty(_path)) return;
        
        string extension = Path.GetExtension(_path).ToLower();
        switch (extension)
        {
            case ".json":
                _loadingDialog.ShowLoading(ImportMain);
                break;
            case ".ranked":
                _loadingDialog.ShowLoading(ImportRanked);
                break;
            case ".csv":
                _loadingDialog.ShowLoading(ImportCSV);
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
            Trace.WriteLine("Error loading player from whitelist");
        }

        if (players == null) return;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var viewModel = new PlayerViewModel(player);
            progress.Report((float)i/players.Count);
            logProgress.Report($"({i+1}/{players.Count}) Checking for duplicates for player: {player.InGameName}");
            if (_tournamentManager.ContainsDuplicatesNoDialog(player)) continue;
            logProgress.Report($"({i+1}/{players.Count}) Adding player: {player.InGameName}");

            viewModel.UpdateHeadBitmap();
            Application.Current.Dispatcher.Invoke(() => { PlayerManager.Add(viewModel); });
        }
        
        logProgress.Report("Loading complete");
        progress.Report(1);
        DialogBox.Show("Done loading data from .json whitelist file");
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
        
            PlayerViewModel player = new() 
            {
                Name = fields[0],
                InGameName = fields[1].Trim(),
                UUID = fields[2].Replace("-", ""),
                PersonalBest = fields[3]
            };
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
            Application.Current.Dispatcher.Invoke(() => { PlayerManager.Add(player); });
            count++;
        }
        
        logProgress.Report("Loading complete");
        progress.Report(1);
        DialogBox.Show("Done loading data from .csv file");
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
     
                PlayerViewModel player = new()
                {
                    Name = loadedPlayer.DisplayName,
                    InGameName = loadedPlayer.IGN.Trim(),
                };
                player.StreamData.SetName(loadedPlayer.Twitch.Trim());
                if (_tournamentManager.ContainsDuplicatesNoDialog(player.Data)) continue;
                     
                logProgress.Report($"({i+1}/{length}) Completing data for {player.InGameName}");
                await player.CompleteData(false);
                
                if (string.IsNullOrEmpty(player.UUID)) playersToComplete.Add(player);
                Application.Current.Dispatcher.Invoke(() => { PlayerManager.Add(player); });
            }
            
            if (playersToComplete.Count > 0)
            {
                await FetchAndAssignUUIDs(playersToComplete, logProgress, cancellationToken);
            }
     
            logProgress.Report("Loading complete");
            progress.Report(1);
            DialogBox.Show("Done loading data from .ranked file");
        }
        catch (Exception ex)
        {
            DialogBox.Show($"Error loading json data: {ex.Message}", "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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

                HttpResponseMessage response = await client.PostAsync(Consts.MojangGetMultipleUUIDAPI, content, cancellationToken);
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