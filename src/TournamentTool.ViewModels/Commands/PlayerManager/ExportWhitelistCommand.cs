using System.Diagnostics;
using System.Text.Json;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Commands.PlayerManager;

public class ExportWhitelistCommand : BaseCommand
{
    private readonly ITournamentManager _tournamentManager;
    private readonly Tournament _tournamentData;
    private readonly IDialogService _dialogService;

    private readonly JsonSerializerOptions _serializerOptions;
    private string _path = string.Empty;
    
    public ExportWhitelistCommand(ITournamentManager tournamentManager, Tournament tournamentData, IDialogService dialogService)
    {
        _tournamentManager = tournamentManager;
        _tournamentData = tournamentData;
        _dialogService = dialogService;

        _serializerOptions = new JsonSerializerOptions() { WriteIndented = true };
    }
    
    public override void Execute(object? parameter)
    {
        _path = _dialogService.ShowOpenFolder();
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

