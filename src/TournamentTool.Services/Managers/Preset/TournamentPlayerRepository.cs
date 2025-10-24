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
    
    void ClearPlayerStreamData();
    void ClearFromController();
    void ClearPlayersFromPOVS();
}

public class TournamentPlayerRepository : ITournamentPlayerRepository, IDisposable
{
    private IDispatcherService Dispatcher { get; }
    private readonly IPlayerViewModelFactory _playerFactory;
    
    private readonly ITournamentState _state;

    private ObservableCollection<IPlayerViewModel> _players { get; } = [];
    public ReadOnlyObservableCollection<IPlayerViewModel> Players { get; }

    
    public TournamentPlayerRepository(ITournamentState state, IPlayerViewModelFactory playerFactory, IDispatcherService dispatcher)
    {
        Dispatcher = dispatcher;
        _state = state;
        _playerFactory = playerFactory;

        Players = new ReadOnlyObservableCollection<IPlayerViewModel>(_players);
        
        _state.PresetChanged += OnPresetChanged;
    }
    public void Dispose()
    {
        _state.PresetChanged -= OnPresetChanged;
    }
    
    private void OnPresetChanged(object? sender, Tournament? tournament)
    {
        _players.Clear();
        if (tournament == null) return;
        
        UpdatePlayers(tournament.Players);
        UpdateTeamNamesForPlayers();
    }
    private void UpdatePlayers(IReadOnlyList<Player> players)
    {
        foreach (var player in players)
        {
            var viewModel = _playerFactory.Create(player);
            _players.Add(viewModel);
        }
    }
    
    public void AddPlayer(IPlayerViewModel player)
    {
        Dispatcher.Invoke(() =>
        {
            _players.Add(player);
            _state.CurrentPreset.Players.Add(player.Data);
            _state.MarkAsModified();
        });
    }
    public void RemovePlayer(IPlayerViewModel player)
    {
        Dispatcher.Invoke(() =>
        {
            _players.Remove(player);
            _state.CurrentPreset.Players.Remove(player.Data);
            _state.MarkAsModified();
        });
    }
    
    public bool ContainsDuplicates(Player findPlayer, Guid? excludeID = null)
    {
        foreach (var player in Players)
        {
            if (excludeID.HasValue && player.Id == excludeID.Value) continue;
            if (player.Equals(findPlayer)) return true;
        }
     
        return false;
    }
    public bool ContainsDuplicatesNoDialog(Player findPlayer, Guid? excludeID = null)
    {
        foreach (var player in Players)
        {
            if (excludeID.HasValue && player.Id == excludeID.Value) continue;
            if (player.EqualsNoDialog(findPlayer)) return true;
        }
    
        return false;
    }
    
    public IPlayerViewModel? GetPlayerByStreamName(string name, StreamType type)
    {
        if (string.IsNullOrEmpty(name)) return null;
        
        int n = Players.Count;
        for (int i = 0; i < n; i++)
        {
            var current = Players[i];
            if ((current.Data.StreamData.ExistName(name) && type == StreamType.twitch) ||
                (current.Data.StreamData.Other.Equals(name, StringComparison.OrdinalIgnoreCase) && current.Data.StreamData.OtherType == type))
                return current;
        }
        return null;
    }
    public IPlayerViewModel? GetPlayerByUUID(string uuid)
    {
        foreach (var player in Players)
        {
            if (!player.UUID.Equals(uuid, StringComparison.OrdinalIgnoreCase)) continue;
            return player;
        }
        
        return null;
    }
    public IPlayerViewModel? GetPlayerByIGN(string ign)
    {
        foreach (var player in Players)
        {
            if (!player.InGameName!.Equals(ign, StringComparison.OrdinalIgnoreCase)) continue;
            return player;
        }
        
        return null;
    }
    
    public void UpdateCategoryForPlayers()
    {
        foreach (var player in Players)
        {
            player.ShowCategory(_state.CurrentPreset is { ShowStreamCategory: true, IsUsingTwitchAPI: true });
        }
    }
    public void UpdateTeamNamesForPlayers()
    {
        foreach (var player in Players)
        {
            player.ShowTeamName(_state.CurrentPreset.IsUsingTeamNames);
        }
    }
    
    public void ClearPlayerStreamData()
    {
        for (int i = 0; i < Players.Count; i++)
            Players[i].ClearStreamData();
    }
    public void ClearFromController()
    {
        for (int i = 0; i < Players.Count; i++)
            Players[i].ClearFromController();
    }
    public void ClearPlayersFromPOVS()
    {
        for (int i = 0; i < Players.Count; i++)
            Players[i].ClearPOVDependencies();
    }
}