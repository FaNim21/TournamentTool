using System.Text.Json.Serialization;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class PointOfView : BaseViewModel
{
    public string? SceneName { get; set; }
    public string? SceneItemName { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public string Text { get; set; }
    [JsonIgnore] public string DisplayedPlayer { get; set; }


    public PointOfView() { }

    public void Update()
    {
        OnPropertyChanged(nameof(DisplayedPlayer));
    }
}
