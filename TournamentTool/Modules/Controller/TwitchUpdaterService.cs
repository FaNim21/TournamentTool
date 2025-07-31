using TournamentTool.Services;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.Modules.Controller;

public class TwitchUpdaterService : ServiceUpdater
{
    private readonly ControllerViewModel _controller;
    private readonly TwitchService _twitch;
    
    
    public TwitchUpdaterService(ControllerViewModel controller, TwitchService twitch)
    {
        _controller = controller;
        _twitch = twitch;
    }
    public void OnEnable()
    {
        
    }
    public void OnDisable()
    {
        
    }
    
    public async Task UpdateAsync(CancellationToken token)
    {
        if (!_twitch.IsConnected || !_controller.IsUsingTwitchAPI) return;
        
        await UpdateStreamDatas();
        _controller.RefreshFilteredCollection();
    }
    private async Task UpdateStreamDatas()
    {
        List<string> logins = [];
        List<StreamDataViewModel> notLivePlayers = [];

        for (int i = 0; i < _controller.TournamentViewModel.Players.Count; i++)
        {
            var current = _controller.TournamentViewModel.Players[i];
            current.StreamData.LiveData.WasUpdated = false;

            if (!string.IsNullOrEmpty(current.StreamData.Main))
                logins.Add(current.StreamData.Main!);
            if (!string.IsNullOrEmpty(current.StreamData.Alt))
                logins.Add(current.StreamData.Alt!);

            notLivePlayers.Add(current.StreamData);
        }

        var streams = await _twitch.GetAllStreamsAsync(logins);
        for (int i = 0; i < streams.Count; i++)
        {
            var current = streams[i];

            for (int j = 0; j < notLivePlayers.Count; j++)
            {
                var streamData = notLivePlayers[j];
                if (!streamData.ExistName(current.UserLogin)) continue;

                bool isMainStream = current.UserLogin.Equals(streamData.Main, StringComparison.OrdinalIgnoreCase);
                bool isAltStream = current.UserLogin.Equals(streamData.Alt, StringComparison.OrdinalIgnoreCase);

                TwitchStreamDataViewModel liveData = new()
                {
                    ID = current.Id,
                    BroadcasterID = current.UserId,
                    UserLogin = current.UserLogin,
                    GameName = current.GameName,
                    StartedAt = current.StartedAt,
                    Language = current.Language,
                    UserName = current.UserName,
                    Title = current.Title,
                    ThumbnailUrl = current.ThumbnailUrl,
                    ViewerCount = current.ViewerCount,
                    Status = current.Type,
                };

                if (isMainStream)
                {
                    streamData.LiveData.Update(liveData);
                    streamData.IsLive = liveData.Status.Equals("live");
                    notLivePlayers.RemoveAt(j);
                    j--;
                }
                else if (isAltStream)
                {
                    streamData.LiveData.Update(liveData);
                    streamData.IsLive = liveData.Status.Equals("live");
                }
            }
        }

        for (int i = 0; i < notLivePlayers.Count; i++)
        {
            var toClear = notLivePlayers[i];
            if (toClear.LiveData.WasUpdated) continue;

            toClear.LiveData.Clear();
        }
        notLivePlayers.Clear();
    }
}