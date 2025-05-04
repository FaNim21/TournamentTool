using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Commands.PlayerManager;

public class ValidatePlayerCommand : BaseCommand
{
    private PlayerManagerViewModel PlayerManager { get; }
    private ILoadingDialog _loadingDialog { get; }
    private IPresetSaver _presetSaver { get; }

    private PlayerViewModel? _player = null;

    
    public ValidatePlayerCommand(PlayerManagerViewModel playerManager, ILoadingDialog loadingDialog, IPresetSaver presetSaver)
    {
        PlayerManager = playerManager;
        _loadingDialog = loadingDialog;
        _presetSaver = presetSaver;
    }
    
    public override void Execute(object? parameter)
    {
        if (parameter is not PlayerViewModel player) return;

        _player = player;
        _loadingDialog.ShowLoading(ValidatePlayer);
    }
    
    private async Task ValidatePlayer(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        if (_player == null) return;
        
        cancellationToken.ThrowIfCancellationRequested();
        progress.Report(0.25f);

        if (string.IsNullOrEmpty(_player.UUID))
        {
            progress.Report(1.0f);
            logProgress.Report($"Cannot validate the player because their UUID is unknown");
            await Task.Delay(1000, cancellationToken);
            return;
        }
        logProgress.Report($"validating player in game name: {_player.InGameName}");
        await Task.Delay(500, cancellationToken);

        var data = await _player.GetDataFromUUID();
        progress.Report(0.5f);
        if (!data.HasValue) return;
        
        if (data.Value.InGameName.Equals(_player.InGameName))
        {
            logProgress.Report($"player {_player.InGameName} is correct");
        }
        else
        {
            logProgress.Report($"incorrect in game name... changing to: {data.Value.InGameName}");
            _player.InGameName = data.Value.InGameName;
        }
        
        await Task.Delay(500, cancellationToken);
        progress.Report(1.0f);
        await Task.Delay(50, cancellationToken);
        _presetSaver.SavePreset();
    }
}