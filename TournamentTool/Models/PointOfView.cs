using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TournamentTool.Commands;
using TournamentTool.Enums;
using TournamentTool.Modules.Controller;
using TournamentTool.Modules.OBS;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models;

public class PointOfViewOBSData
{
    public int ID { get; init; }
    public string GroupName { get; init; }
    public string SceneName { get; init; }
    public string SceneItemName { get; init; }
    
    public string TextFieldItemName { get; set; } = string.Empty;
    public string HeadItemName { get; set; } = string.Empty;
    public string PersonalBestItemName { get; set; } = string.Empty;
    
    public bool IsFromWhiteList { get; set; }
    public string DisplayedPlayer { get; set; } = string.Empty;
    public string PersonalBest { get; set; } = string.Empty;
    public string HeadViewParametr { get; set; } = string.Empty;
    public StreamDisplayInfo StreamDisplayInfo { get; set; } = new(string.Empty, StreamType.twitch);

    public int Volume { get; set; }
    public bool IsMuted { get; set; } = true;
    
    
    public PointOfViewOBSData(int id, string groupName, string sceneName, string sceneItemName)
    {
        ID = id;
        GroupName = groupName;
        SceneName = sceneName;
        SceneItemName = sceneItemName;
    }
}

public class PointOfView : BaseViewModel
{
    private readonly IPointOfViewOBSController _controller;

    public SceneType Type { get; }
    public PointOfViewOBSData Data { get; }
    
    public int OriginWidth { get; init; }
    public int Width { get; private set; }
    public int OriginHeight { get; init; }
    public int Height { get; private set; }

    public int OriginX { get; init; }
    public int X { get; private set; }
    public int OriginY { get; init; }
    public int Y { get; private set; }

    public string TextFieldItemName
    {
        get => Data.TextFieldItemName;
        set
        {
            Data.TextFieldItemName = value;
            OnPropertyChanged(nameof(TextFieldItemName));
        }
    }
    public string HeadItemName
    {
        get => Data.HeadItemName;
        set
        {
            Data.HeadItemName = value;
            OnPropertyChanged(nameof(HeadItemName));
        }
    }
    public string PersonalBestItemName
    {
        get => Data.PersonalBestItemName;
        set
        {
            Data.PersonalBestItemName = value;
            OnPropertyChanged(nameof(PersonalBestItemName));
        }
    }

    public bool IsFromWhiteList
    {
        get => Data.IsFromWhiteList;
        set => Data.IsFromWhiteList = value;
    }
    public string DisplayedPlayer
    {
        get => Data.DisplayedPlayer;
        set
        {
            Data.DisplayedPlayer = value;
            OnPropertyChanged(nameof(DisplayedPlayer));
        }
    }
    public string PersonalBest
    {
        get => Data.PersonalBest;
        set => Data.PersonalBest = value;
    }
    public string HeadViewParametr
    {
        get => Data.HeadViewParametr;
        set => Data.HeadViewParametr = value;
    }
    public StreamDisplayInfo StreamDisplayInfo
    {
        get => Data.StreamDisplayInfo;
        set => Data.StreamDisplayInfo = value;
    }

    public bool IsFocused { get; private set; }
    public Brush? BackgroundColor { get; set; }

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

    public string TextVolume => $"{NewVolume}%";

    public int Volume
    {
        get => Data.Volume;
        set
        {
            Data.Volume = value;
            OnPropertyChanged(nameof(Volume));
        }
    }

    public bool IsMuted
    {
        get => Data.IsMuted;
        set
        {
            Data.IsMuted = value;  
            OnPropertyChanged(nameof(IsMuted));
        } 
    }
    
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


    public PointOfView(IPointOfViewOBSController controller, PointOfViewOBSData data, SceneType type = SceneType.Main)
    {
        Data = data;
        Type = type;
        _controller = controller;

        UnFocus();

        ApplyVolumeCommand = new RelayCommand(ApplyVolume);
        RefreshCommand = new RelayCommand(async () => { await Refresh(); });
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

    public void SetCustomPOV(IPovUsage? other = null)
    {
        if (string.IsNullOrEmpty(CustomStreamName) && !string.IsNullOrEmpty(CurrentCustomStreamName))
        {
            CurrentCustomStreamName = string.Empty;
            Clear();
            return;
        }
        if (CustomStreamName.Equals(CurrentCustomStreamName) && CustomStreamType == CurrentCustomStreamType) return;
        
        if (player != null) Clear();
        CurrentCustomStreamName = CustomStreamName;
        CurrentCustomStreamType = CustomStreamType;

        CustomPlayer customPlayer = new(new StreamDisplayInfo(CustomStreamName, _customStreamType), "Unk", string.Empty);
        CustomPlayerViewModel playerViewModel = new CustomPlayerViewModel(customPlayer, other);
        
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
        
        UpdatePOVInfo();
    }
    
    private void UpdatePOVInfo()
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

        _controller.SendOBSInformations(Data);
    }
    
    public bool Swap(PointOfView? pov)
    {
        if (pov is null) return false;
        if (Type != pov.Type) return false;
        
        IPlayer? povPlayer = pov.player;

        (pov.CustomStreamName, CustomStreamName) = (CustomStreamName, pov.CustomStreamName);
        (pov.CustomStreamType, CustomStreamType) = (CustomStreamType, pov.CustomStreamType);
        (pov.CurrentCustomStreamName, CurrentCustomStreamName) = (CurrentCustomStreamName, pov.CurrentCustomStreamName);
        (pov.CurrentCustomStreamType, CurrentCustomStreamType) = (CurrentCustomStreamType, pov.CurrentCustomStreamType);

        pov.player = player;
        pov.UpdatePOVInfo();
        player = povPlayer;
        UpdatePOVInfo();
        return true;
    }

    public async Task Refresh()
    {
        _controller.SetBrowserURL(Data.SceneItemName, string.Empty);
        await Task.Delay(25);
        UpdatePOVInfo();
    }

    public void Focus()
    {
        IsFocused = true;
        Application.Current?.Dispatcher.Invoke(() => { BackgroundColor = new SolidColorBrush(Color.FromRgb(153, 224, 255)); });
        OnPropertyChanged(nameof(BackgroundColor));
    }
    public void UnFocus()
    {
        IsFocused = false;
        Application.Current?.Dispatcher.Invoke(() => { BackgroundColor = new SolidColorBrush(Color.FromRgb(102, 179, 204)); });
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

        _controller.UpdatePOVBrowser(Data);
    }

    public void Clear(bool fullClear = false)
    {
        DisplayedPlayer = string.Empty;
        StreamDisplayInfo = new StreamDisplayInfo(string.Empty, StreamType.twitch);
        Volume = 0;
        HeadViewParametr = string.Empty;
        PersonalBest = string.Empty;

        if (fullClear)
        {
            ClearCustomData();
        }
        if (player != null)
        {
            IsPlayerUsed = false;
            player = null;
        }

        _controller.Clear(Data);
    }

    private void ClearCustomData()
    {
        CurrentCustomStreamName = string.Empty;
        CurrentCustomStreamType = StreamType.twitch;
        CustomStreamName = string.Empty;
        CustomStreamType = StreamType.twitch;
    }
}