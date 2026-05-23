using System.Runtime.CompilerServices;

namespace TournamentTool.Services.Managers.Preset;

public interface INotifyPresetModification
{
    void MarkAsModified([CallerMemberName] string? propertyName = null);
    void MarkAsUnmodified();
}