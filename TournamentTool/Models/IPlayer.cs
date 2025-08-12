using TournamentTool.Enums;

namespace TournamentTool.Models;

public record StreamDisplayInfo(string Name, StreamType Type);

public interface IPlayer
{
    public bool IsLive { get; }

    public bool IsUsedInPov { get; set; }
    public bool IsUsedInPreview { get; set; }

    public string DisplayName { get; }
    public string GetPersonalBest { get; }
    public string HeadViewParameter { get; }
    public StreamDisplayInfo StreamDisplayInfo { get; }
    public bool IsFromWhitelist { get; }
}
