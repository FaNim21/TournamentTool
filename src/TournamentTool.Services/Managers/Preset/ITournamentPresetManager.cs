using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;

namespace TournamentTool.Services.Managers.Preset;

public interface ITournamentPresetManager : IPresetInfo, INotifyPresetModification
{
    bool IsCurrentlyOpened { get; }
    
    event Action<ControllerMode, bool>? OnControllerModeChanged;
    event Action<Tournament>? OnPresetChanged;

    void ChangeData(Tournament? tournament);
    void ChangeName(string newName);

    void PresetIsSaved();

    bool IsNullOrEmpty();
    Tournament GetData();

    void Delete();
    void Clear();
}

/*
public interface ITournamentManager
{
    string Name { get; set; }

    // ObservableCollection<PlayerViewModel> Players { get; set; }
    
    bool ContainsDuplicates(Domain.Entities.Player findPlayer, Guid? excludeID = null);
    bool ContainsDuplicatesNoDialog(Domain.Entities.Player findPlayer, Guid? excludeID = null);
}
*/
