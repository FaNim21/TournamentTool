using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ObsWebSocket.Core.Protocol.Common;
using ObsWebSocket.Core.Protocol.Responses;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Parsers;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Obs;
using TournamentTool.Presentation.Entities;
using TournamentTool.Services.Logging;

namespace TournamentTool.Presentation.Obs.Entities;

public class PointOfView : BrowserItem, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private SceneType _type;
    public SceneType Type
    {
        get => _type;
        init => SetField(ref _type, value);
    }

    private string _displayedPlayer = string.Empty;
    public string DisplayedPlayer
    {
        get => _displayedPlayer;
        set => SetField(ref _displayedPlayer, value);
    }

    public bool IsPlayerUsed
    {
        get
        {
            if (Player == null) return false;
            if (Type == SceneType.Main)
            {
                return Player.IsUsedInPov;
            }

            return Player.IsUsedInPreview;
        }
        set
        {
            if (Player == null) return;
            if (Type == SceneType.Main)
            {
                Player.IsUsedInPov = value;
            }
            else
            {
                Player.IsUsedInPreview = value;
            }
        }
    }
    public bool IsEmpty => Player == null;

    private IPlayer? _player;
    public IPlayer? Player
    {
        get => _player;
        set => SetField(ref _player, value);
    }

    private string _customStreamName = string.Empty;
    public string CustomStreamName
    {
        get => _customStreamName;
        set => SetField(ref _customStreamName, value);
    }
    
    private StreamType _customStreamType = StreamType.twitch;
    public StreamType CustomStreamType
    {
        get => _customStreamType;
        set => SetField(ref _customStreamType, value);
    }

    private string _currentCustomStreamName = string.Empty;
    public string CurrentCustomStreamName
    {
        get => _currentCustomStreamName;
        set => SetField(ref _currentCustomStreamName, value);
    }
    
    private StreamType _currentCustomStreamType = StreamType.twitch;
    public StreamType CurrentCustomStreamType
    {
        get => _currentCustomStreamType;
        set => SetField(ref _currentCustomStreamType, value);
    }
    
    public StreamDisplayInfo StreamDisplayInfo { get; private set; } = new(string.Empty, StreamType.twitch);

    private bool _isMuted = true;
    public bool IsMuted
    {
        get => _isMuted;
        set => SetField(ref _isMuted, value);
    }
    
    private int _volume;
    public int Volume
    {
        get => _volume;
        set => SetField(ref _volume, value);
    }
    
    private int _newVolume;
    public int NewVolume
    {
        get => _newVolume;
        set => SetField(ref _newVolume, value);
    }

    private BindingKey keyHead { get; set; } = BindingKey.Empty();
    private BindingKey keyDisplayName { get; set; } = BindingKey.Empty();
    private BindingKey keyIgn { get; set; } = BindingKey.Empty();
    private BindingKey keyPb { get; set; } = BindingKey.Empty();
    private BindingKey keyTeamName { get; set; } = BindingKey.Empty();
    private BindingKey keyStreamName { get; set; } = BindingKey.Empty();
    private BindingKey keyStreamType { get; set; } = BindingKey.Empty();


    public PointOfView(ISceneManager sceneManager, ILoggingService logger, SceneType type) : base(sceneManager, logger)
    {
        Type = type;
        InputKind = InputKind.tt_point_of_view;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        
        if (Player == null) return;
        
        Player.IsUsedInPov = false;
        Player.IsUsedInPreview = false;
        StreamDisplayInfo = new StreamDisplayInfo(string.Empty, StreamType.twitch);
    }

    public override void Initialize(IScene scene, SceneItemStub item, SceneItemStub? group = null, SceneItemConfiguration? configuration = null)
    {
        base.Initialize(scene, item, group, configuration);
        
        keyHead = BindingKey.New("POV", "head", SourceName);
        keyDisplayName = BindingKey.New("POV", "display_name", SourceName);
        keyIgn = BindingKey.New("POV", "ign", SourceName);
        keyPb = BindingKey.New("POV", "pb", SourceName);
        keyTeamName = BindingKey.New("POV", "team_name", SourceName);
        keyStreamName = BindingKey.New("POV", "stream_name", SourceName);
        keyStreamType = BindingKey.New("POV", "stream_type", SourceName);
    }
    public override async Task LoadAsync()
    {
        (string? currentName, int volume, StreamType type) data = await GetBrowserURLStreamInfo(SourceUUID);

        bool specificPovExists = Scene.ExistInItems<PointOfView>(p => 
            p.StreamDisplayInfo.Name.Equals(data.currentName, StringComparison.OrdinalIgnoreCase)
            && p.StreamDisplayInfo.Type.Equals(data.type));
        
        if (string.IsNullOrEmpty(data.currentName) || specificPovExists)
        {
            Clear(true);
            return;
        }

        IPlayerViewModel? foundPlayer = SceneManager.GetPlayerByStreamName(data.currentName, data.type);

        ChangeVolume(data.volume);

        if (foundPlayer == null)
        {
            CustomStreamType = data.type;
            CustomStreamName = data.currentName;
            SetCustomPOV();
            return;
        }

        if (foundPlayer is not IPlayer playerViewModel) return;
        SetPOV(playerViewModel);
    }

    public void SetCustomPOV(IPovUsage? other = null)
    {
        if (string.IsNullOrEmpty(CustomStreamName) && !string.IsNullOrEmpty(CurrentCustomStreamName))
        {
            CurrentCustomStreamName = string.Empty;
            Clear();
            return;
        }
        if (CustomStreamName.Equals(CurrentCustomStreamName) && CustomStreamType == CurrentCustomStreamType) return;
        
        if (Player != null) Clear();
        CurrentCustomStreamName = CustomStreamName;
        CurrentCustomStreamType = CustomStreamType;

        CustomPlayerData customPlayerData = new(new StreamDisplayInfo(CustomStreamName, CustomStreamType), "Unk", string.Empty);
        CustomPlayer playerViewModel = new(customPlayerData, other);
        
        SetPlayerToPOV(playerViewModel);
    }

    public void SetPOV(IPlayer? povInfo)
    {
        if (povInfo is null)
        {
            Clear();
            return;
        }

        if (povInfo.IsFromWhitelist)
        {
            SetPlayerToPOV(povInfo);
            ClearCustomData();
        }
        else
        {
            CustomStreamName = povInfo.StreamDisplayInfo.Name;
            CustomStreamType = povInfo.StreamDisplayInfo.Type;
            SetCustomPOV(povInfo);
        }
    }
    private void SetPlayerToPOV(IPlayer? povInfo)
    {
        var oldPlayer = Player;
        Player = povInfo;
        if (IsPlayerUsed)
        {
            Player = oldPlayer;
            return;
        }
        
        if (oldPlayer != null)
        {
            if (Type == SceneType.Main)
            {
                oldPlayer.IsUsedInPov = false;
            }
            else
            {
                oldPlayer.IsUsedInPreview = false;
            }
        }
        
        UpdatePOVInfoAsync();
    }
    
    public bool Swap(PointOfView? pov)
    {
        if (pov is null) return false;
        if (Type != pov.Type) return false;
        
        IPlayer? povPlayer = pov.Player;

        (pov.CustomStreamName, CustomStreamName) = (CustomStreamName, pov.CustomStreamName);
        (pov.CustomStreamType, CustomStreamType) = (CustomStreamType, pov.CustomStreamType);
        (pov.CurrentCustomStreamName, CurrentCustomStreamName) = (CurrentCustomStreamName, pov.CurrentCustomStreamName);
        (pov.CurrentCustomStreamType, CurrentCustomStreamType) = (CurrentCustomStreamType, pov.CurrentCustomStreamType);

        pov.Player = Player;
        pov.UpdatePOVInfoAsync();
        Player = povPlayer;
        UpdatePOVInfoAsync();
        return true;
    }

    public void UpdateUrl()
    {
        Url = GetURL();
    }
    
    private void UpdatePOVInfoAsync()
    {
        if (Player == null)
        {
            Clear();
            return;
        }

        DisplayedPlayer = Player.DisplayName;
        StreamDisplayInfo = Player.StreamDisplayInfo;
        IsPlayerUsed = true;

        UpdateUrl();
        Update();
        UpdateBindings();
    }

    public void UpdateBindings()
    {
        string headUrl = string.Empty;
        if (Player != null)
        {
            headUrl = SceneManager.GetHeadURL(Player.HeadViewParameter, 180);
            if (string.IsNullOrEmpty(Player.HeadViewParameter)) headUrl = string.Empty;
        }
        
        //TODO: 0 trzeba zrobic publish kolejke z racji aktualizacji wielu scene item na raz, dla wydajnosci trzeba zrobic grupowe aktualizowanie
        SceneManager.Publish(keyHead, headUrl);
        SceneManager.Publish(keyDisplayName, DisplayedPlayer);
        SceneManager.Publish(keyIgn, Player?.InGameName ?? string.Empty);
        SceneManager.Publish(keyPb, Player?.GetPersonalBest ?? string.Empty);
        SceneManager.Publish(keyTeamName, Player?.TeamName ?? string.Empty);
        SceneManager.Publish(keyStreamName, Player?.StreamDisplayInfo.Name ?? string.Empty);
        SceneManager.Publish(keyStreamType, Player == null ? string.Empty : Player.StreamDisplayInfo.Type);
    }
    
    public override async Task RefreshAsync()
    {
        Url = string.Empty;
        Inputs["url"] = Url;
        await ForceUpdateAsync();
        
        await Task.Delay(25);
        UpdatePOVInfoAsync();
    }

    public void ChangeVolume(int volume)
    {
        Volume = volume;

        if (NewVolume == Volume) return;
        NewVolume = Volume;
        IsMuted = Volume == 0;
    }

    public override void Clear(bool fullClear = false)
    {
        DisplayedPlayer = string.Empty;
        StreamDisplayInfo = new StreamDisplayInfo(string.Empty, StreamType.twitch);
        Volume = 0;

        if (fullClear)
        {
            ClearCustomData();
        }
        if (Player != null)
        {
            IsPlayerUsed = false;
            Player = null;
        }

        base.Clear(fullClear);
        UpdateBindings();
    }

    private void ClearCustomData()
    {
        CurrentCustomStreamName = string.Empty;
        CurrentCustomStreamType = StreamType.twitch;
        CustomStreamName = string.Empty;
        CustomStreamType = StreamType.twitch;
    }
    
    private string GetURL()
    {
        if (string.IsNullOrEmpty(StreamDisplayInfo.Name)) return string.Empty;

        int muted = IsMuted ? 1 : 0;
        string url = StreamDisplayInfo.Type switch
        {
            StreamType.twitch => $"https://player.twitch.tv/?channel={StreamDisplayInfo.Name}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&quality=chunked&volume={(Volume / 100f).ToString(CultureInfo.InvariantCulture)}",
            StreamType.kick => $"https://player.kick.com/{StreamDisplayInfo.Name}?muted={IsMuted.ToString().ToLower()}&allowfullscreen=true",
            StreamType.youtube => $"https://www.youtube.com/embed/{StreamDisplayInfo.Name}?autoplay=1&controls=0&mute={muted}",
            _ => string.Empty
        };

        return url;
    }
    
    public async Task<(string?, int, StreamType)> GetBrowserURLStreamInfo(string sourceUuid)
    {
        GetInputSettingsResponseData? settingsResponse = await SceneManager.GetItemInputSettingsAsync(sourceUuid);
        if (settingsResponse == null ||
            !settingsResponse.InputSettings.HasValue ||
            !settingsResponse.InputSettings.Value.TryGetProperty("url", out JsonElement urlElement)) 
            return (string.Empty, 0, StreamType.twitch);

        string url = urlElement.ToString();
        if (string.IsNullOrEmpty(url))return (string.Empty, 0, StreamType.twitch); 

        try
        {
            return StreamUrlParser.Parse(url);
        } 
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        
        return (string.Empty, 0, StreamType.twitch);
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) 
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}