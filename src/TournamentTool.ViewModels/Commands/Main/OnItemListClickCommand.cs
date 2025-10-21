using TournamentTool.Domain.Interfaces;

namespace TournamentTool.ViewModels.Commands.Main;

public class OnItemListClickCommand : BaseCommand
{
    public IPresetSaver PresetSaver { get; set; }

    public OnItemListClickCommand(IPresetSaver presetSaver)
    {
        PresetSaver = presetSaver;
    }

    public override void Execute(object? parameter)
    {
        PresetSaver.SavePreset();
    }
}
