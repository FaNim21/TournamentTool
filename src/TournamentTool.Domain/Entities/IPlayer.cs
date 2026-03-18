using TournamentTool.Domain.Enums;

namespace TournamentTool.Domain.Entities;

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
    public string InGameName { get; }
    public string TeamName { get; }
    public string GetPersonalBest { get; }
    public string HeadViewParameter { get; }
    public StreamDisplayInfo StreamDisplayInfo { get; }
    public bool IsFromWhitelist { get; }
}
