using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Interfaces;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels.Commands.PlayerManager;

public class FixPlayerHeadCommand : BaseCommand
{
    private PlayerManagerViewModel PlayerManager { get; }
    private readonly IPresetSaver _presetSaver;
    private readonly IWindowService _windowService;

    private PlayerViewModel? _player;


    public FixPlayerHeadCommand(PlayerManagerViewModel playerManager, IPresetSaver presetSaver, IWindowService windowService)
    {
        PlayerManager = playerManager;
        _presetSaver = presetSaver;
        _windowService = windowService;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not PlayerViewModel player) return;
        _player = player;

        _windowService.ShowLoading(FixPlayerHead);
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