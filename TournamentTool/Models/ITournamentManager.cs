using System.Collections.ObjectModel;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models;

public interface ITournamentManager
{
    string Name { get; set; }

    ObservableCollection<PlayerViewModel> Players { get; set; }
    
    bool ContainsDuplicates(Player findPlayer, Guid? excludeID = null);
    bool ContainsDuplicatesNoDialog(Player findPlayer, Guid? excludeID = null);

    void AddPlayer(PlayerViewModel playerViewModel);
    void RemovePlayer(PlayerViewModel playerViewModel);
}