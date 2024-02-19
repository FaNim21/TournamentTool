
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class Player : BaseViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Name { get; set; }
    public string? TwitchName { get; set; }
    public string? PersonalBest { get; set; }


    public Player(string name = "")
    {
        Name = name;
    }

    public void Update()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(TwitchName));
        OnPropertyChanged(nameof(PersonalBest));
    }

    internal void Clear()
    {
        Name = string.Empty;
        TwitchName = string.Empty;
        PersonalBest = string.Empty;
    }
}
