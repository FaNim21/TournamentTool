using System.Collections.ObjectModel;
using TournamentTool.Core.Extensions;
using TournamentTool.Core.Factories;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;

namespace TournamentTool.Services.Managers.Preset;

public sealed class TournamentPlayerRepository : ITournamentPlayerRepository, IDisposable
{
    private readonly IDispatcherService _dispatcher;
    private readonly IPlayerViewModelFactory _playerFactory;
    private readonly ITwitchService _twitchService;
    private readonly ITournamentState _state;
    private readonly Settings _settings;

    private ObservableCollection<IPlayerViewModel> _players { get; } = [];
    public ReadOnlyObservableCollection<IPlayerViewModel> Players { get; }


    public TournamentPlayerRepository(ITournamentState state, IPlayerViewModelFactory playerFactory, IDispatcherService dispatcher,
        ITwitchService twitchService, ISettingsProvider settingsProvider)
    {
        _dispatcher = dispatcher;
        _state = state;
        _playerFactory = playerFactory;
        _twitchService = twitchService;
        _settings = settingsProvider.Get<Settings>();

        Players = new ReadOnlyObservableCollection<IPlayerViewModel>(_players);
        
        _state.PresetChanged += OnPresetChanged;
    }
    public void Dispose()
    {
        _state.PresetChanged -= OnPresetChanged;
    }
    
    private void OnPresetChanged(object? sender, Tournament? tournament)
    {
        _dispatcher.Invoke(() =>
        {
            _players.Clear();
            if (tournament == null) return;
        
            foreach (var player in tournament.Players)
            {
                var viewModel = _playerFactory.Create(player);
                _players.Add(viewModel);
            }
            
            UpdateTeamNamesForPlayers();
        }, CustomDispatcherPriority.Background);
    }
    
    public void AddPlayer(IPlayerViewModel player)
    {
        _dispatcher.Invoke(() =>
        {
            _players.Add(player);
            _state.CurrentPreset.Players.Add(player.Data);
            _state.MarkAsModified();
        });
    }
    public void RemovePlayer(IPlayerViewModel player)
    {
        _dispatcher.Invoke(() =>
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
            player.ShowCategory(_state.CurrentPreset is { ShowStreamCategory: true } && _twitchService.IsConnected);
        }
    }
    public void UpdateTeamNamesForPlayers()
    {
        foreach (var player in Players)
        {
            player.ShowTeamName(_state.CurrentPreset.IsUsingTeamNames);
        }
    }
    
    public Func<string>? GetBindingFieldValue(IPlayer? player, string field)
    {
        //TEMP do momentu jak stwierdze ze chce to dynamicznie w dictionary
        if (player is null) return null;

        return field switch
        {
            "head" => () => _settings.HeadAPIType.GetHeadURL(player.HeadViewParameter, 180),
            "display_name" => () => player.DisplayName,
            "ign" => () => player.InGameName,
            "pb" => () => player.GetPersonalBest,
            "team_name" => () => player.TeamName,
            "stream_name" => () => player.StreamDisplayInfo.Name,
            "stream_type" => () => player.StreamDisplayInfo.Type.ToString(),
            _ => null
        };
    }
}