using System.Globalization;
using TournamentTool.Core.Extensions;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Configuration;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.ViewModels.Selectable.Controller;

public interface IPointOfViewOBSController
{
    void SetBrowserURL(string itemName, string data);
    void SetTextField(string itemName, string data);
    void UpdatePOVBrowser(PointOfViewOBSData data);
    void SendOBSInformations(PointOfViewOBSData data);
    void Clear(PointOfViewOBSData data);
}

public class PointOfViewOBSController : IPointOfViewOBSController
{
    private readonly ObsController _obs;
    private readonly ITournamentState _tournamentState;
    
    private readonly Domain.Entities.Settings _settings;
    


    public PointOfViewOBSController(ObsController obs, ITournamentState tournamentState, ISettingsProvider settingsProvider)
    {
        _obs = obs;
        _tournamentState = tournamentState;
        
        _settings = settingsProvider.Get<Domain.Entities.Settings>();
    }

    public void UpdatePOVBrowser(PointOfViewOBSData data)
    {
        if (!_obs.SetBrowserURL(data.SceneItemName, GetURL(data))) return;
    }
    public void SendOBSInformations(PointOfViewOBSData data)
    {
        if (!_obs.SetBrowserURL(data.SceneItemName, GetURL(data))) return;

        string headUrl = _settings.HeadAPIType.GetHeadURL(data.HeadViewParametr, 180);
        if (string.IsNullOrEmpty(data.HeadViewParametr) || _tournamentState is { CurrentPreset.SetPovHeadsInBrowser: false })
        {
            headUrl = string.Empty;
        }
        SetBrowserURL(data.HeadItemName, headUrl);

        string displayName = _tournamentState.CurrentPreset.DisplayedNameType switch
        {
            DisplayedNameType.Twitch => data.StreamDisplayInfo.Name,
            DisplayedNameType.IGN => data.IsFromWhiteList ? data.HeadViewParametr : data.StreamDisplayInfo.Name,
            DisplayedNameType.WhiteList => data.DisplayedPlayer,
            _ => string.Empty
        };
        SetTextField(data.TextFieldItemName, displayName);

        SetTextField(data.PersonalBestItemName, _tournamentState.CurrentPreset.SetPovPBText ? data.PersonalBest : string.Empty);
    }

    public void SetBrowserURL(string itemName, string data)
    {
        if (string.IsNullOrEmpty(itemName)) return;
        _obs.SetBrowserURL(itemName, data);
    }
    public void SetTextField(string itemName, string data)
    {
        if (string.IsNullOrEmpty(itemName)) return;
        _obs.SetTextField(itemName, data);
    } 
    
    private string GetURL(PointOfViewOBSData data)
    {
        if (string.IsNullOrEmpty(data.StreamDisplayInfo.Name)) return string.Empty;

        int muted = data.IsMuted ? 1 : 0;
        string url = data.StreamDisplayInfo.Type switch
        {
            StreamType.twitch => $"https://player.twitch.tv/?channel={data.StreamDisplayInfo.Name}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&quality=chunked&volume={(data.Volume / 100f).ToString(CultureInfo.InvariantCulture)}",
            StreamType.kick => $"https://player.kick.com/{data.StreamDisplayInfo.Name}?muted={data.IsMuted.ToString().ToLower()}&allowfullscreen=true",
            StreamType.youtube => $"https://www.youtube.com/embed/{data.StreamDisplayInfo.Name}?autoplay=1&controls=0&mute={muted}",
            _ => string.Empty
        };

        return url;
    }

    public void Clear(PointOfViewOBSData data)
    {
        SetBrowserURL(data.SceneItemName, string.Empty);
        SetBrowserURL(data.HeadItemName, string.Empty);
        SetTextField(data.TextFieldItemName, string.Empty);
        SetTextField(data.PersonalBestItemName, string.Empty);
    }
}