using TournamentTool.Models;

namespace TournamentTool.ViewModels.Entities;

public class CustomPlayerViewModel : IPlayer
{
    private readonly CustomPlayer _data;
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
    public string GetPersonalBest => _data.PersonalBest;
    public string HeadViewParameter => _data.HeadViewParameter;
    public StreamDisplayInfo StreamDisplayInfo => _data.StreamDisplayInfo;
    public bool IsFromWhitelist => false;
    
    
    public CustomPlayerViewModel(CustomPlayer data, IPovUsage? other)
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