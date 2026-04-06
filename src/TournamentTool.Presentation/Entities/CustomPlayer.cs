using TournamentTool.Domain.Entities;

namespace TournamentTool.Presentation.Entities;

public class CustomPlayer : IPlayer
{
    private readonly CustomPlayerData _data;
    private readonly IPovUsage _other;

    public bool IsLive => true;
    public bool IsUsedInPov
    {
        get => _other.IsUsedInPov;
        set => _other.IsUsedInPov = value;
    }
    public bool IsUsedInPreview 
    {
        get => _other.IsUsedInPreview;
        set => _other.IsUsedInPreview = value;
    }
    public string DisplayName => _data.StreamDisplayInfo.Name;
    public string InGameName => string.Empty;
    public string TeamName => string.Empty;
    public string GetPersonalBest => _data.PersonalBest;
    public string HeadViewParameter => _data.HeadViewParameter;
    public StreamDisplayInfo StreamDisplayInfo => _data.StreamDisplayInfo;
    public bool IsFromWhitelist => false;
    
    
    public CustomPlayer(CustomPlayerData data, IPovUsage? other)
    {
        _data = data;
        _other = other ?? new EmptyPlayerViewModel();
    }
}

public class EmptyPlayerViewModel : IPovUsage
{
    public bool IsUsedInPov { get; set; }
    public bool IsUsedInPreview { get; set; }
}