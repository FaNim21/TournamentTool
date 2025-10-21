using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.ViewModels.Selectable;
using TournamentTool.ViewModels.Selectable.Preset;

namespace TournamentTool.ViewModels.Commands.Main;

public class RemovePresetCommand : BaseCommand
{
    private readonly IDialogService _dialogService;
    private PresetManagerViewModel PresetManager { get; }

    public RemovePresetCommand(PresetManagerViewModel presetManager, IDialogService dialogService)
    {
        _dialogService = dialogService;
        PresetManager = presetManager;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not TournamentPresetViewModel tournament) return;

        var result = _dialogService.Show($"Are you sure you want to delete: {tournament.Name}", "Deleting", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        
        PresetManager.RemoveItem(tournament);
    }
}
