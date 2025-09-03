using System.Collections.ObjectModel;
using TournamentTool.Models;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Interfaces;

public interface INotifyPresetModification
{
    void PresetIsModified();
}

public interface IPresetInfo
{
    public string Name { get; }
    public bool IsPresetModified { get; }
}

public interface ITournamentManager
{
    string Name { get; set; }

    ObservableCollection<PlayerViewModel> Players { get; set; }
    
    bool ContainsDuplicates(Player findPlayer, Guid? excludeID = null);
    bool ContainsDuplicatesNoDialog(Player findPlayer, Guid? excludeID = null);
}