using System.Collections.ObjectModel;

namespace TournamentTool.Models;

public interface ITournamentManager : IPreset
{
    ObservableCollection<Player> Players { get; set; }
    
    bool ContainsDuplicates(Player findPlayer, Guid? excludeID = null);
    bool ContainsDuplicatesNoDialog(Player findPlayer, Guid? excludeID = null);

    void AddPlayer(Player player);
    void RemovePlayer(Player player);
}