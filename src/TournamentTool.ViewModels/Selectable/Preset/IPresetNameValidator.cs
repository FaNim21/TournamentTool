namespace TournamentTool.ViewModels.Selectable.Preset;

public interface IPresetNameValidator
{
    bool IsPresetNameUnique(string name);
}