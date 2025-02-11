using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.PlayerManager;

public class FixPlayerHeadCommand : BaseCommand
{
    private PlayerManagerViewModel PlayerManager { get; }
    private readonly ILoadingDialog _loadingDialog;
    private readonly IPresetSaver _presetSaver;

    private Player? _player = null;


    public FixPlayerHeadCommand(PlayerManagerViewModel playerManager, ILoadingDialog loadingDialog, IPresetSaver presetSaver)
    {
        PlayerManager = playerManager;
        _loadingDialog = loadingDialog;
        _presetSaver = presetSaver;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not Player player) return;
        _player = player;

        _loadingDialog.ShowLoading(FixPlayerHead);
    }
    
    private async Task FixPlayerHead(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
            
        progress.Report(0.5f);
        logProgress.Report($"Checking skin to update {_player!.InGameName} head");
        await _player.ForceUpdateHeadImage();
        progress.Report(1);
        _presetSaver.SavePreset();
    }
    
}