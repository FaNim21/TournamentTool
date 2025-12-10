using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Services;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Entities.Player;

namespace TournamentTool.ViewModels.Selectable.Controller.Hub;

public class TwitchUpdaterService : IServiceUpdater, IServiceUpdaterTimer
{
    private readonly ControllerViewModel _controller;
    private readonly ITwitchService _twitch;
    private readonly ITournamentPlayerRepository _playerRepository;


    public TwitchUpdaterService(ControllerViewModel controller, ITwitchService twitch, ITournamentPlayerRepository playerRepository)
    {
        _controller = controller;
        _twitch = twitch;
        _playerRepository = playerRepository;
    }
    public void OnEnable()
    {
        
    }
    public void OnDisable()
    {
        
    }
    
    public async Task UpdateAsync(CancellationToken token)
    {
        if (!_twitch.IsConnected) return;
        
        await UpdateStreamDatas();
        _controller.RefreshFilteredCollection();
    }
    private async Task UpdateStreamDatas()
    {
        List<string> logins = [];
        List<StreamDataViewModel> notLivePlayers = [];

        for (int i = 0; i < _playerRepository.Players.Count; i++)
        {
            IPlayerViewModel current = _playerRepository.Players[i];
            if (current is not PlayerViewModel currentPlayer) continue;
            
            currentPlayer.StreamData.Live.WasUpdated = false;

            if (!string.IsNullOrEmpty(currentPlayer.StreamData.Main))
                logins.Add(currentPlayer.StreamData.Main!);
            if (!string.IsNullOrEmpty(currentPlayer.StreamData.Alt))
                logins.Add(currentPlayer.StreamData.Alt!);

            notLivePlayers.Add(currentPlayer.StreamData);
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

                TwitchStreamData liveData = new()
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
                    streamData.Live.Update(liveData);
                    streamData.IsLive = liveData.Status.Equals("live");
                    notLivePlayers.RemoveAt(j);
                    j--;
                }
                else if (isAltStream)
                {
                    streamData.Live.Update(liveData);
                    streamData.IsLive = liveData.Status.Equals("live");
                }
            }
        }

        for (int i = 0; i < notLivePlayers.Count; i++)
        {
            var toClear = notLivePlayers[i];
            if (toClear.Live.WasUpdated) continue;

            toClear.Live.Clear();
        }
        notLivePlayers.Clear();
    }

    public void UpdateTimer(string time)
    {
        _controller.TwitchUpdateProgressText = time;
    }
}