using System.Globalization;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.Modules.OBS;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.Controller;

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
    private readonly TournamentViewModel _tournament;

    
    public PointOfViewOBSController(ObsController obs, TournamentViewModel tournament)
    {
        _obs = obs;
        _tournament = tournament;
    }

    public void UpdatePOVBrowser(PointOfViewOBSData data)
    {
        if (!_obs.SetBrowserURL(data.SceneItemName, GetURL(data))) return;
    }
    public void SendOBSInformations(PointOfViewOBSData data)
    {
        if (!_obs.SetBrowserURL(data.SceneItemName, GetURL(data))) return;

        if (_tournament.SetPovHeadsInBrowser)
        {
            // string path = $"https://mc-heads.net/avatar/{data.HeadViewParametr}/180";
            string path = $"minotar.net/helm/{data.HeadViewParametr}/180.png";
            if (string.IsNullOrEmpty(data.HeadViewParametr))
                path = string.Empty;

            SetBrowserURL(data.HeadItemName, path);
        }

        if (_tournament.DisplayedNameType != DisplayedNameType.None)
        {
            string name = _tournament.DisplayedNameType switch
            {
                DisplayedNameType.Twitch => data.StreamDisplayInfo.Name,
                DisplayedNameType.IGN => data.IsFromWhiteList ? data.HeadViewParametr : data.StreamDisplayInfo.Name,
                DisplayedNameType.WhiteList => data.DisplayedPlayer,
                _ => string.Empty
            };

            SetTextField(data.TextFieldItemName, name);
        }

        if (_tournament.SetPovPBText)
        {
            SetTextField(data.PersonalBestItemName, data.PersonalBest);
        }
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