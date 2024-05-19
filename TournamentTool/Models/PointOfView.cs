using System.Numerics;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class PointOfView : BaseViewModel
{
    public string? SceneName { get; set; }
    public string? SceneItemName { get; set; }
    public int ID { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public Brush? BackgroundColor { get; set; }

    [JsonIgnore] public string Text { get; set; } = string.Empty;
    [JsonIgnore] public string DisplayedPlayer { get; set; } = string.Empty;
    [JsonIgnore] public string TwitchName { get; set; } = string.Empty;
    [JsonIgnore] public float Volume { get; set; } = 0;



    public PointOfView()
    {
        UnFocus();
    }

    public void Update()
    {
        OnPropertyChanged(nameof(DisplayedPlayer));
        OnPropertyChanged(nameof(Text));
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
    }

    public string GetURL()
    {
        if (string.IsNullOrEmpty(TwitchName)) return string.Empty;

        return $"https://player.twitch.tv/?channel={TwitchName}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&quality=chunked&volume={Volume}";
    }

    public void Clear()
    {
        DisplayedPlayer = string.Empty;
        Text = string.Empty;
        TwitchName = string.Empty;

        Update();
    }
}
