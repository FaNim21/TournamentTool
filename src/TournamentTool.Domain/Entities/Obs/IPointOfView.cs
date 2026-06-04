using TournamentTool.Domain.Enums;

namespace TournamentTool.Domain.Entities.Obs;

public interface IPointOfView
{
    public SceneType Type { get; }

    public string DisplayedPlayer { get; set; }

    public bool IsPlayerUsed { get; set; }
    public bool IsEmpty { get; }

    public IPlayer? Player { get; set; }

    public string CustomStreamName { get; set; }

    public StreamType CustomStreamType { get; set; }

    public string CurrentCustomStreamName { get; set; }

    public StreamType CurrentCustomStreamType { get; set; }

    public StreamDisplayInfo StreamDisplayInfo { get; }

    public bool IsMuted { get; set; }

    public int Volume { get; set; }

    public int NewVolume { get; set; }
}