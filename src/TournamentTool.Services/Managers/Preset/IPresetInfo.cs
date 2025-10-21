namespace TournamentTool.Services.Managers.Preset;

public interface IPresetInfo
{
    public string Name { get; }
    public bool IsPresetModified { get; }
}