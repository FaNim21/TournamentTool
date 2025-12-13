using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels.Commands.PlayerManager;

public class RemovePlayerCommand : BaseCommand
{
    private readonly PlayerManagerViewModel _playerManager;
    private readonly IPresetSaver _presetSaver;
    private readonly IDialogService _dialogService;


    public RemovePlayerCommand(PlayerManagerViewModel playerManager, IPresetSaver presetSaver, IDialogService dialogService)
    {
        _playerManager = playerManager;
        _presetSaver = presetSaver;
        _dialogService = dialogService;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not PlayerViewModel player) return;
        
        MessageBoxResult result = _dialogService.Show($"Are you sure you want to remove player: {player.InGameName}", "Removing player", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        _playerManager.Remove(player);
        _playerManager.RefreshFilteredCollectionView();
        _presetSaver.SavePreset();
    }
}
