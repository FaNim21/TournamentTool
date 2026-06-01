using System.Runtime.CompilerServices;

namespace TournamentTool.Services.Managers.Preset;

public interface INotifyPresetModification
{
    void MarkAsModified([CallerFilePath] string? filePath = null, [CallerMemberName] string? propertyName = null);
    void MarkAsUnmodified();
}