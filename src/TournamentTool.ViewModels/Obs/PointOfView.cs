using System.Globalization;
using System.Windows.Input;
using ObsWebSocket.Core.Protocol.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels.Obs;

public class PointOfView : BrowserItemViewModel
{
    public SceneType Type { get; }

    public override int ZIndex { get; protected set; } = 0;

    public bool IsFromWhiteList { get; private set; }

    private string _displayedPlayer = string.Empty;
    public string DisplayedPlayer
    {
        get => _displayedPlayer;
        set
        {
            _displayedPlayer = value;
            OnPropertyChanged(nameof(DisplayedPlayer));
        }
    }

    public StreamDisplayInfo StreamDisplayInfo { get; private set; } = new(string.Empty, StreamType.twitch);

    public bool IsPlayerUsed
    {
        get
        {
            if (player == null) return false;
            if (Type == SceneType.Main)
            {
                return player.IsUsedInPov;
            }

            return player.IsUsedInPreview;
        }
        set
        {
            if (player == null) return;
            if (Type == SceneType.Main)
            {
                player.IsUsedInPov = value;
            }
            else
            {
                player.IsUsedInPreview = value;
            }
        }
    }
    public bool IsEmpty => player == null;

    public IPlayer? player;

    private string _customStreamName = string.Empty;
    public string CustomStreamName
    {
        get => _customStreamName;
        set
        {
            _customStreamName = value;
            OnPropertyChanged(nameof(CustomStreamName));
        }
    }

    private StreamType _customStreamType;
    public StreamType CustomStreamType
    {
        get => _customStreamType;
        set
        {
            _customStreamType = value;
            OnPropertyChanged(nameof(CustomStreamType));
        }
    }

    public string CurrentCustomStreamName { get; private set; } = string.Empty;
    public StreamType CurrentCustomStreamType { get; private set; } = StreamType.twitch;

    private int _volume;
    public int Volume
    {
        get => _volume;
        set
        {
            _volume = value;
            OnPropertyChanged(nameof(Volume));
        }
    }

    private bool _isMuted = true;
    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            _isMuted = value;  
            OnPropertyChanged(nameof(IsMuted));
        } 
    }
    
    public string TextVolume => $"{NewVolume}%";
    private int _newVolume;
    public int NewVolume
    {
        get => _newVolume;
        set
        {
            _newVolume = value;
            OnPropertyChanged(nameof(NewVolume));
            OnPropertyChanged(nameof(TextVolume));
        }
    }

    public ICommand ApplyVolumeCommand { get; set; }
    public ICommand RefreshCommand { get; set; }


    public PointOfView(ISceneController controller, IDispatcherService dispatcher, ILoggingService logger, SceneType type = SceneType.Main) 
        : base(controller, dispatcher, logger)
    {
        Type = type;
        
        BackgroundColor = Consts.UnFocusedPovColor;

        ApplyVolumeCommand = new RelayCommand(async () => await ApplyVolume());
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
    }

    public override async Task InitializeAsync(IScene scene, SceneItemStub item, SceneItemStub? group = null)
    {
        await base.InitializeAsync(scene, item, group);
        
        (string? currentName, int volume, StreamType type) data = (string.Empty, 0, StreamType.twitch);
        try
        {
            data = await Controller.GetBrowserURLStreamInfo(SourceUUID);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        bool specificPovExists = scene.ExistInItems<PointOfView>(p => 
            p.StreamDisplayInfo.Name.Equals(data.currentName, StringComparison.OrdinalIgnoreCase)
            && p.StreamDisplayInfo.Type.Equals(data.type));
        
        if (string.IsNullOrEmpty(data.currentName) || specificPovExists)
        {
            await ClearAsync(true);
            return;
        }

        IPlayerViewModel? foundPlayer = Controller.GetPlayerByStreamName(data.currentName, data.type);

        ChangeVolume(data.volume);

        if (foundPlayer != null)
        {
            if (foundPlayer is PlayerViewModel playerViewModel)
            {
                await SetPOVAsync(playerViewModel);
            }
        }
        else
        {
            CustomStreamType = data.type;
            CustomStreamName = data.currentName;
            await SetCustomPOVAsync();
        }
    }

    public async Task SetCustomPOVAsync(IPovUsage? other = null)
    {
        if (string.IsNullOrEmpty(CustomStreamName) && !string.IsNullOrEmpty(CurrentCustomStreamName))
        {
            CurrentCustomStreamName = string.Empty;
            await ClearAsync();
            return;
        }
        if (CustomStreamName.Equals(CurrentCustomStreamName) && CustomStreamType == CurrentCustomStreamType) return;
        
        if (player != null) await ClearAsync();
        CurrentCustomStreamName = CustomStreamName;
        CurrentCustomStreamType = CustomStreamType;

        CustomPlayer customPlayer = new(new StreamDisplayInfo(CustomStreamName, _customStreamType), "Unk", string.Empty);
        CustomPlayerViewModel playerViewModel = new(customPlayer, other);
        
        await SetPlayerToPOV(playerViewModel);
    }

    public async Task SetPOVAsync(IPlayer? povInfo)
    {
        if (povInfo is null)
        {
            await ClearAsync();
            return;
        }

        if (povInfo.IsFromWhitelist)
        {
            await SetPlayerToPOV(povInfo);
            ClearCustomData();
        }
        else
        {
            CustomStreamName = povInfo.StreamDisplayInfo.Name;
            CustomStreamType = povInfo.StreamDisplayInfo.Type;
            await SetCustomPOVAsync(povInfo);
        }
    }
    private async Task SetPlayerToPOV(IPlayer? povInfo)
    {
        var oldPlayer = player;
        player = povInfo;
        if (IsPlayerUsed)
        {
            player = oldPlayer;
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
        
        await UpdatePOVInfoAsync();
    }
    
    private async Task UpdatePOVInfoAsync()
    {
        if (player == null)
        {
            await ClearAsync();
            return;
        }

        DisplayedPlayer = player.DisplayName;
        StreamDisplayInfo = player.StreamDisplayInfo;
        IsFromWhiteList = player.IsFromWhitelist;
        IsPlayerUsed = true;

        Url = GetURL();
        await UpdateAsync();
    }
    
    public async Task<bool> SwapAsync(PointOfView? pov)
    {
        if (pov is null) return false;
        if (Type != pov.Type) return false;
        
        IPlayer? povPlayer = pov.player;

        (pov.CustomStreamName, CustomStreamName) = (CustomStreamName, pov.CustomStreamName);
        (pov.CustomStreamType, CustomStreamType) = (CustomStreamType, pov.CustomStreamType);
        (pov.CurrentCustomStreamName, CurrentCustomStreamName) = (CurrentCustomStreamName, pov.CurrentCustomStreamName);
        (pov.CurrentCustomStreamType, CurrentCustomStreamType) = (CurrentCustomStreamType, pov.CurrentCustomStreamType);

        pov.player = player;
        await pov.UpdatePOVInfoAsync();
        player = povPlayer;
        await UpdatePOVInfoAsync();
        return true;
    }

    public override async Task RefreshAsync()
    {
        Url = string.Empty;
        await UpdateAsync();
        
        await Task.Delay(25);
        await UpdatePOVInfoAsync();
    }

    public void ChangeVolume(int volume)
    {
        Volume = volume;

        if (NewVolume == Volume) return;
        NewVolume = Volume;
        IsMuted = Volume == 0;
    }
    public async Task ApplyVolume()
    {
        Volume = NewVolume;
        IsMuted = Volume == 0;

        await UpdateAsync();
    }

    public override async Task ClearAsync(bool fullClear = false)
    {
        DisplayedPlayer = string.Empty;
        StreamDisplayInfo = new StreamDisplayInfo(string.Empty, StreamType.twitch);
        Volume = 0;

        if (fullClear)
        {
            ClearCustomData();
        }
        if (player != null)
        {
            IsPlayerUsed = false;
            player = null;
        }

        await base.ClearAsync(fullClear);
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
}