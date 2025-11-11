using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.Managers.Preset;

public interface ITournamentState : INotifyPresetModification
{
    Tournament CurrentPreset { get; }
    bool IsModified { get; }
    bool IsCurrentlyOpened { get; }

    event EventHandler<Tournament?>? PresetChanged;
    event EventHandler<bool>? ModificationStateChanged;
    event EventHandler<string>? PresetNameChanged;

    void ChangePreset(Tournament? newPreset);
    void DeletePreset();
    
    void ChangeName(string newName);

    bool IsEmpty();
}