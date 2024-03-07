using System.Text.Json.Serialization;
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

    public string Text { get; set; } = string.Empty;
    [JsonIgnore] public string DisplayedPlayer { get; set; } = string.Empty;
    [JsonIgnore] public string TwitchName { get; set; } = string.Empty;



    public PointOfView() { }

    public void Update()
    {
        OnPropertyChanged(nameof(DisplayedPlayer));
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
}
