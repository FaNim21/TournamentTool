using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TournamentTool.Commands;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class PointOfView : BaseViewModel
{
    private readonly ControllerViewModel _controller;

    public string? SceneName { get; set; }
    public string? SceneItemName { get; set; }
    public int ID { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public string TextFieldItemName { get; set; }

    public Brush? BackgroundColor { get; set; }

    public string Text { get; set; } = string.Empty;
    public string DisplayedPlayer { get; set; } = string.Empty;
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


    public PointOfView(ControllerViewModel controller)
    {
        _controller = controller;

        UnFocus();

        ApplyVolumeCommand = new RelayCommand(ApplyVolume);
    }

    public void Update()
    {
        OnPropertyChanged(nameof(DisplayedPlayer));
        OnPropertyChanged(nameof(TextFieldItemName));
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

    public void Swap(PointOfView pov)
    {
        string tempDisplayedPlayer = pov.DisplayedPlayer;
        string tempTwitchName = pov.TwitchName;
        pov.DisplayedPlayer = DisplayedPlayer;
        pov.TwitchName = TwitchName;
        DisplayedPlayer = tempDisplayedPlayer;
        TwitchName = tempTwitchName;
        Update();
    }

    public void Focus()
    {
        Application.Current.Dispatcher.Invoke(() => { BackgroundColor = new SolidColorBrush(Color.FromRgb(190, 239, 255)); });
        OnPropertyChanged(nameof(BackgroundColor));
    }
    public void UnFocus()
    {
        Application.Current.Dispatcher.Invoke(() => { BackgroundColor = new SolidColorBrush(Color.FromRgb(162, 203, 217)); });
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

        _controller.SetBrowserURL(this);
    }

    public void UpdateNameTextField()
    {
        if (string.IsNullOrEmpty(TextFieldItemName)) return;

        _controller.SetTextField(TextFieldItemName, TwitchName);
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

        Update();
    }
}
