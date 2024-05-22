using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TournamentTool.Commands;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Controller;

namespace TournamentTool.Models;

public class PointOfView : BaseViewModel
{
    private readonly ObsController _obs;

    public string? SceneName { get; set; }
    public string? SceneItemName { get; set; }
    public int ID { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public string TextFieldItemName { get; set; } = string.Empty;
    public string HeadItemName { get; set; } = string.Empty;

    public Brush? BackgroundColor { get; set; }

    public string Text { get; set; } = string.Empty;
    public string DisplayedPlayer { get; set; } = string.Empty;
    public string HeadViewParametr { get; set; } = string.Empty;
    public string TwitchName { get; set; } = string.Empty;

    public float Volume { get; set; } = 0;
    public string TextVolume { get => $"{(int)(NewVolume * 100)}%"; }

    private float _newVolume;
    public float NewVolume
    {
        get => _newVolume;
        set
        {
            if (_newVolume != value)
                _newVolume = value;
            OnPropertyChanged(nameof(NewVolume));
            OnPropertyChanged(nameof(TextVolume));
        }
    }

    public ICommand ApplyVolumeCommand { get; set; }


    public PointOfView(ObsController obs)
    {
        _obs = obs;

        UnFocus();

        ApplyVolumeCommand = new RelayCommand(ApplyVolume);
    }

    public void Update()
    {
        OnPropertyChanged(nameof(DisplayedPlayer));
        OnPropertyChanged(nameof(TextFieldItemName));
        OnPropertyChanged(nameof(HeadItemName));
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Volume));
    }
    public void UpdateTransform()
    {
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));

        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));
    }

    public void SetPOV(string DisplayedName, string twitchName, string headInfoParametr)
    {
        DisplayedPlayer = DisplayedName;
        TwitchName = twitchName;
        HeadViewParametr = headInfoParametr;

        SetHead();
        Update();
        _obs.SetBrowserURL(this);
    }
    public void Swap(PointOfView pov)
    {
        string tempDisplayedPlayer = pov.DisplayedPlayer;
        string tempTwitchName = pov.TwitchName;
        string tempHeadViewParametr = pov.HeadViewParametr;

        pov.SetPOV(DisplayedPlayer, TwitchName, HeadViewParametr);
        SetPOV(tempDisplayedPlayer, tempTwitchName, tempHeadViewParametr);
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
    }
    public void ApplyVolume()
    {
        Volume = NewVolume;
        Update();

        _obs.SetBrowserURL(this);
    }

    public void SetHead()
    {
        if (string.IsNullOrEmpty(HeadViewParametr) || string.IsNullOrEmpty(HeadItemName)) return;

        string path = $"minotar.net/helm/{HeadViewParametr}/180.png";
        _obs.SetBrowserURL(HeadItemName, path);
    }

    public void UpdateNameTextField()
    {
        if (string.IsNullOrEmpty(TextFieldItemName)) return;

        _obs.SetTextField(TextFieldItemName, TwitchName);
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
        HeadViewParametr = string.Empty;

        _obs.SetBrowserURL(this);
        _obs.SetBrowserURL(HeadItemName, string.Empty);

        Update();
    }
}
