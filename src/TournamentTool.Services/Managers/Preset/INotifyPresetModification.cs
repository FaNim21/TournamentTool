namespace TournamentTool.Services.Managers.Preset;

public interface INotifyPresetModification
{
    void MarkAsModified();
    void MarkAsUnmodified();
}