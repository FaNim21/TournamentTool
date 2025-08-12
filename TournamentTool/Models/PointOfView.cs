using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TournamentTool.Commands;
using TournamentTool.Enums;
using TournamentTool.Modules.OBS;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models;

public class PointOfView : BaseViewModel
{
    private readonly ObsController _obs;
    private readonly TournamentViewModel _tournament;

    public Scene Scene { get; set; }

    public string? GroupName { get; set; }
    public string? SceneName { get; set; }
    public string? SceneItemName { get; set; }
    public int ID { get; set; }

    public int OriginWidth { get; set; }
    public int Width { get; set; }
    public int OriginHeight { get; set; }
    public int Height { get; set; }

    public int OriginX { get; set; }
    public int X { get; set; }
    public int OriginY { get; set; }
    public int Y { get; set; }

    public string TextFieldItemName { get; set; } = string.Empty;
    public string HeadItemName { get; set; } = string.Empty;
    public string PersonalBestItemName { get; set; } = string.Empty;

    private bool _isResizing;
    public bool IsResizing
    {
        get => _isResizing;
        set
        {
            if (_isResizing == value) return;

            _isResizing = value;
            OnPropertyChanged(nameof(IsResizing));
        }
    }

    public Brush? BackgroundColor { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsPlayerUsed
    {
        get
        {
            if (player == null) return false;
            if (Scene.Type == SceneType.Main)
            {
                return player.IsUsedInPov;
            }
            else
            {
                return player.IsUsedInPreview;
            }
        }
        set
        {
            if (player == null) return;
            if (Scene.Type == SceneType.Main)
            {
                player.IsUsedInPov = value;
            }
            else
            {
                player.IsUsedInPreview = value;
            }
        }
    }

    public bool IsFromWhiteList { get; set; }

    public IPlayer? player;
    public string DisplayedPlayer { get; set; } = string.Empty;
    public string PersonalBest { get; set; } = string.Empty;
    public string HeadViewParametr { get; set; } = string.Empty;
    public StreamDisplayInfo StreamDisplayInfo { get; set; } = new("", StreamType.twitch);

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

    private string _currentCustomStreamName = string.Empty;
    private StreamType _currentCustomStreamType = StreamType.twitch;
    
    public int Volume { get; set; } = 0;
    public string TextVolume => $"{NewVolume}%";
    public string URLVolume => (Volume / 100f).ToString(CultureInfo.InvariantCulture);

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

    public ICommand ApplyVolumeCommand { get; set; }
    public ICommand RefreshCommand { get; set; }


    public PointOfView(ObsController obs, TournamentViewModel tournament, Scene scene, string? groupName = "")
    {
        _obs = obs;
        _tournament = tournament;
        Scene = scene;

        UnFocus();

        ApplyVolumeCommand = new RelayCommand(ApplyVolume);
        RefreshCommand = new RelayCommand(async () => { await Refresh(); });

        GroupName = groupName;
    }

    public void Update()
    {
        OnPropertyChanged(nameof(DisplayedPlayer));
        OnPropertyChanged(nameof(TextFieldItemName));
        OnPropertyChanged(nameof(PersonalBestItemName));
        OnPropertyChanged(nameof(HeadItemName));
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Volume));
    }
    public void UpdateTransform(float proportion)
    {
        X = (int)(OriginX / proportion);
        Y = (int)(OriginY / proportion);

        Width = (int)(OriginWidth / proportion);
        Height = (int)(OriginHeight / proportion);

        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));

        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));
    }

    public void SetBrowserURL()
    {
        if (!_obs.SetBrowserURL(SceneItemName!, GetURL())) return;
        
        if (_tournament.SetPovHeadsInBrowser) UpdateHead();
        if (_tournament.DisplayedNameType != DisplayedNameType.None) UpdateNameTextField();
        if (_tournament.SetPovPBText) UpdatePersonalBestTextField();
    }
    
    public void SetCustomPOV()
    {
        if (string.IsNullOrEmpty(CustomStreamName) && !string.IsNullOrEmpty(_currentCustomStreamName))
        {
            _currentCustomStreamName = string.Empty;
            Clear();
            return;
        }
        if (CustomStreamName.Equals(_currentCustomStreamName) && CustomStreamType == _currentCustomStreamType) return;
        
        if (player != null) Clear();
        _currentCustomStreamName = CustomStreamName;
        _currentCustomStreamType = CustomStreamType;

        PlayerViewModel customPlayer = new();
        customPlayer.Name = CustomStreamName;
        if (_currentCustomStreamType == StreamType.twitch)
        {
            customPlayer.StreamData.Main = CustomStreamName;
        }
        else
        {
            customPlayer.StreamData.Other = CustomStreamName;
            customPlayer.StreamData.OtherType = CustomStreamType;
        }
        
        SetPOV(customPlayer, true);
    }
    public void SetPOV(IPlayer? povInfo, bool isCustom = false)
    {
        var oldPlayer = player;
        player = povInfo;
        if (player == null)
        {
            Clear();
            return;
        }

        if ((IsFromWhiteList && IsPlayerUsed) || Scene.IsPlayerInPov(player!.StreamDisplayInfo.Name))
        {
            player = oldPlayer;
            return;
        }

        if (oldPlayer != null)
        {
            if (Scene.Type == SceneType.Main)
            {
                oldPlayer.IsUsedInPov = false;
            }
            else
            {
                oldPlayer.IsUsedInPreview = false;
            }
        }

        if (!isCustom)
        {
            _currentCustomStreamName = string.Empty;
            CustomStreamName = string.Empty;
        }
        IsResizing = true;
        IsPlayerUsed = true;
        SetPOV();
    }
    private void SetPOV()
    {
        if (player == null)
        {
            Clear();
            return;
        }

        DisplayedPlayer = player.DisplayName;
        StreamDisplayInfo = player.StreamDisplayInfo;
        HeadViewParametr = player.HeadViewParameter;
        PersonalBest = player.GetPersonalBest ?? "Unk";
        IsFromWhiteList = player.IsFromWhitelist;
        IsPlayerUsed = true;

        SetBrowserURL();
        Update();
    }
    public void Swap(PointOfView pov)
    {
        IPlayer? povPlayer = pov.player;

        (pov.CustomStreamName, CustomStreamName) = (CustomStreamName, pov.CustomStreamName);
        (pov.CustomStreamType, CustomStreamType) = (CustomStreamType, pov.CustomStreamType);
        (pov._currentCustomStreamName, _currentCustomStreamName) = (_currentCustomStreamName, pov._currentCustomStreamName);
        (pov._currentCustomStreamType, _currentCustomStreamType) = (_currentCustomStreamType, pov._currentCustomStreamType);

        pov.player = player;
        pov.SetPOV();
        player = povPlayer;
        SetPOV();
    }

    public async Task Refresh()
    {
        _obs.SetBrowserURL(SceneItemName!, "");
        await Task.Delay(25);
        SetPOV();
    }

    public void Focus()
    {
        Application.Current.Dispatcher.Invoke(() => { BackgroundColor = new SolidColorBrush(Color.FromRgb(153, 224, 255)); });
        OnPropertyChanged(nameof(BackgroundColor));
    }
    public void UnFocus()
    {
        Application.Current.Dispatcher.Invoke(() => { BackgroundColor = new SolidColorBrush(Color.FromRgb(102, 179, 204)); });
        OnPropertyChanged(nameof(BackgroundColor));
    }

    public void ChangeVolume(int volume)
    {
        Volume = volume;

        if (NewVolume == Volume) return;
        NewVolume = Volume;
        IsMuted = Volume == 0;
    }
    public void ApplyVolume()
    {
        Volume = NewVolume;
        IsMuted = Volume == 0;
        Update();

        SetBrowserURL();
    }

    public void UpdateHead()
    {
        if (string.IsNullOrEmpty(HeadItemName) || string.IsNullOrEmpty(HeadViewParametr)) return;

        string path = $"minotar.net/helm/{HeadViewParametr}/180.png";
        if (string.IsNullOrEmpty(HeadViewParametr))
            path = string.Empty;

        _obs.SetBrowserURL(HeadItemName, path);
    }
    private void ClearHead()
    {
        if (string.IsNullOrEmpty(HeadItemName)) return;
        _obs.SetBrowserURL(HeadItemName, "");
    }

    public void UpdateNameTextField()
    {
        if (string.IsNullOrEmpty(TextFieldItemName)) return;

        string name = _tournament.DisplayedNameType switch
        {
            DisplayedNameType.Twitch => StreamDisplayInfo.Name,
            DisplayedNameType.IGN => IsFromWhiteList ? HeadViewParametr : StreamDisplayInfo.Name,
            DisplayedNameType.WhiteList => DisplayedPlayer,
            _ => string.Empty
        };

        _obs.SetTextField(TextFieldItemName, name);
    }
    private void ClearNameTextField()
    {
        if (string.IsNullOrEmpty(TextFieldItemName)) return;
        _obs.SetTextField(TextFieldItemName, "");
    }

    public void UpdatePersonalBestTextField()
    {
        if (string.IsNullOrEmpty(PersonalBestItemName)) return;
        _obs.SetTextField(PersonalBestItemName, PersonalBest);
    }
    private void ClearPersonalBestTextField()
    {
        if (string.IsNullOrEmpty(PersonalBestItemName)) return;
        _obs.SetTextField(PersonalBestItemName, "");
    }

    public string GetURL()
    {
        if (string.IsNullOrEmpty(StreamDisplayInfo.Name)) return string.Empty;

        int muted = IsMuted ? 1 : 0;
        string url = StreamDisplayInfo.Type switch
        {
            StreamType.twitch => $"https://player.twitch.tv/?channel={StreamDisplayInfo.Name}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&quality=chunked&volume={URLVolume}",
            StreamType.kick => $"https://player.kick.com/{StreamDisplayInfo.Name}?muted={IsMuted.ToString().ToLower()}&allowfullscreen=true",
            StreamType.youtube => $"https://www.youtube.com/embed/{StreamDisplayInfo.Name}?autoplay=1&controls=0&mute={muted}",
            _ => string.Empty
        };

        return url;
    }
    
    public void Clear(bool fullClear = false)
    {
        DisplayedPlayer = string.Empty;
        Text = string.Empty;
        StreamDisplayInfo = new StreamDisplayInfo("", StreamType.twitch);
        Volume = 0;
        HeadViewParametr = string.Empty;
        PersonalBest = string.Empty;

        if (fullClear)
        {
            _currentCustomStreamName = string.Empty;
            CustomStreamName = string.Empty;
        }

        if (player != null)
        {
            IsPlayerUsed = false;
            player = null;
        }

        if (!_obs.SetBrowserURL(SceneItemName!, GetURL())) return;
        ClearHead();
        ClearNameTextField();
        ClearPersonalBestTextField();

        Update();
    }

    public bool IsEmpty()
    {
        return player == null;
    }
}