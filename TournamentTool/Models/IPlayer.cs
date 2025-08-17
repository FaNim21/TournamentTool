using TournamentTool.Enums;

namespace TournamentTool.Models;

public record StreamDisplayInfo(string Name, StreamType Type);

public interface IPovUsage
{
    public bool IsUsedInPov { get; set; }
    public bool IsUsedInPreview { get; set; }
}

public interface IPlayer : IPovUsage
{
    public bool IsLive { get; }

    public string DisplayName { get; }
    public string GetPersonalBest { get; }
    public string HeadViewParameter { get; }
    public StreamDisplayInfo StreamDisplayInfo { get; }
    public bool IsFromWhitelist { get; }
}
