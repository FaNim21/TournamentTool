using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TournamentTool.Commands;
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
    public string TwitchName { get; set; } = string.Empty;

    public float Volume { get; set; } = 0;
    public string TextVolume { get => $"{(int)(NewVolume * 100)}%"; }

    private bool _isMuted;
    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            Trace.WriteLine(value + " muted");
            _isMuted = value;
            OnPropertyChanged(nameof(IsMuted));
        }
    }

    private float _newVolume;
    public float NewVolume
    {
        get => _newVolume;
        set
        {
            if (_newVolume != value) _newVolume = value;
            OnPropertyChanged(nameof(NewVolume));
            OnPropertyChanged(nameof(TextVolume));
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

    public void SetPOV(IPlayer? povInfo)
    {
        var oldPlayer = player;
        player = povInfo;
        if (player == null)
        {
            Clear();
            return;
        }

        if ((IsFromWhiteList && IsPlayerUsed) || Scene.IsPlayerInPov(player!.TwitchName))
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
        TwitchName = player.TwitchName;
        HeadViewParametr = player.HeadViewParameter;
        PersonalBest = player.GetPersonalBest ?? "Unk";
        IsFromWhiteList = player.IsFromWhitelist;
        IsPlayerUsed = true;

        _obs.SetBrowserURL(this);
        Update();
    }
    public void Swap(PointOfView pov)
    {
        IPlayer? povPlayer = pov.player;

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

    public void ChangeVolume(float volume)
    {
        Volume = Math.Clamp(volume, 0, 1);

        if (NewVolume == volume) return;
        NewVolume = Volume;
        IsMuted = Volume != 0f;
    }
    public void ApplyVolume()
    {
        Volume = NewVolume;
        IsMuted = Volume != 0f;
        Update();

        _obs.SetBrowserURL(this);
    }

    public void UpdateHead()
    {
        if (string.IsNullOrEmpty(HeadItemName)) return;

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

        string name = string.Empty;
        switch (_tournament.DisplayedNameType)
        {
            case DisplayedNameType.Twitch:
                name = TwitchName;
                break;
            case DisplayedNameType.IGN:
                name = IsFromWhiteList ? HeadViewParametr : TwitchName;
                break;
            case DisplayedNameType.WhiteList:
                name = DisplayedPlayer;
                break;
        }

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
        if (string.IsNullOrEmpty(TwitchName)) return string.Empty;

        return $"https://player.twitch.tv/?channel={TwitchName}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&quality=chunked&volume={Volume.ToString().Replace(',', '.')}";
    }

    public void Clear()
    {
        DisplayedPlayer = string.Empty;
        Text = string.Empty;
        TwitchName = string.Empty;
        Volume = 0;
        HeadViewParametr = string.Empty;
        PersonalBest = string.Empty;

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
