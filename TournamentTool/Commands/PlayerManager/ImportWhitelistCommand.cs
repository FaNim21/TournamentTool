using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using TournamentTool.Components.Controls;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels;

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
        catch { }
        if (players == null) return;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            progress.Report((float)i/players.Count);
            logProgress.Report($"({i+1}/{players.Count}) Checking for duplicates for player: {player.InGameName}");
            if (_tournamentManager.ContainsDuplicatesNoDialog(player)) continue;
            logProgress.Report($"({i+1}/{players.Count}) Adding player: {player.InGameName}");

            player.UpdateHeadBitmap();
            Application.Current.Dispatcher.Invoke(() => { PlayerManager.Add(player); });
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
        
            Player data = new() 
            {
                Name = fields[0].Trim(),
                InGameName = fields[1].Trim(),
                UUID = fields[2],
                PersonalBest = fields[3]
            };
            if (fields.Length > 4) 
            {
                data.StreamData.SetName(fields[4].ToLower().Trim());
                if (fields.Length > 5)
                    data.StreamData.SetName(fields[5].ToLower().Trim());
            }

            if (_tournamentManager.ContainsDuplicatesNoDialog(data))
            {
                totalLines--;
                continue;
            }
                    
            logProgress.Report($"({count+1}/{totalLines}) Completing data for {data.InGameName}");
            await data.CompleteData();
            Application.Current.Dispatcher.Invoke(() => { PlayerManager.Add(data); });
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
            JSONPlayer[] players = JsonSerializer.Deserialize<JSONPlayer[]>(json)!;
            if (players.Length == 0) return;
     
            int length = players.Length;
            for (int i = 0; i < length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report((float)i / length);
                     
                var player = players[i];
     
                Player data = new()
                {
                    Name = player.DisplayName.Trim(),
                    InGameName = player.IGN.Trim(),
                };
                data.StreamData.SetName(player.Twitch.Trim());
                if (_tournamentManager.ContainsDuplicatesNoDialog(data)) continue;
                     
                logProgress.Report($"({i+1}/{length}) Completing data for {data.InGameName}");
                await data.UpdateHeadImage();
                Application.Current.Dispatcher.Invoke(() => { PlayerManager.Add(data); });
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
}

