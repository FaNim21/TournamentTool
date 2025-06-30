using System.Diagnostics;
using System.IO;
using System.Text.Json;
using TournamentTool.Components.Controls;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.Commands.PlayerManager;

public class ExportWhitelistCommand : BaseCommand
{
    private readonly ITournamentManager _tournamentManager;
    private readonly Tournament _tournamentData;

    private readonly JsonSerializerOptions _serializerOptions;
    private string _path = string.Empty;
    
    public ExportWhitelistCommand(ITournamentManager tournamentManager, Tournament tournamentData)
    {
        _tournamentManager = tournamentManager;
        _tournamentData = tournamentData;
        
        _serializerOptions = new JsonSerializerOptions() { WriteIndented = true };
    }
    
    public override void Execute(object? parameter)
    {
        _path = DialogBox.ShowOpenFolder();
        if (string.IsNullOrEmpty(_path)) return;
        
        var data = JsonSerializer.Serialize<object>(_tournamentData.Players, _serializerOptions);
        string date = DateTimeOffset.Now.ToString("HH.mm_dd-MM-yyyy");
        string fileName = $"{_tournamentManager.Name}-Whitelist {date}.json";
        
        int count = 1;
        while (File.Exists(Consts.AppdataPath + "\\" + fileName))
        {
            fileName = $"{_tournamentManager.Name}-Whitelist {date} [{count}].json";
            count++;
        }

        string finalPath = Path.Combine(_path, fileName);
        File.WriteAllText(finalPath, data);
        
        Process.Start(new ProcessStartInfo
        {
            FileName = _path,
            UseShellExecute = true,
            Verb = "open"
        });
    }
}

