using System.Collections.ObjectModel;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;

namespace TournamentTool.Services.Managers.Preset;

public interface ITournamentPlayerRepository
{
    ReadOnlyObservableCollection<IPlayerViewModel> Players { get; }

    void AddPlayer(IPlayerViewModel player);
    void RemovePlayer(IPlayerViewModel player);

    bool ContainsDuplicates(Player findPlayer, Guid? excludeID = null);
    bool ContainsDuplicatesNoDialog(Player findPlayer, Guid? excludeID = null);

    IPlayerViewModel? GetPlayerByStreamName(string name, StreamType type);
    IPlayerViewModel? GetPlayerByUUID(string uuid);
    IPlayerViewModel? GetPlayerByIGN(string ign);

    void UpdateCategoryForPlayers();
    void UpdateTeamNamesForPlayers();
    
    Func<string>? GetBindingFieldValue(IPlayer? player, string field);
}