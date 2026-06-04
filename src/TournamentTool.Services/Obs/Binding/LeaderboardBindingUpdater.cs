using TournamentTool.Core.Extensions;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Obs.Binding;

public interface ILeaderboardBindingUpdater
{
    void Publish(int startIndex, int endIndex);
    void Publish(LeaderboardEntry entry);
}

public sealed class LeaderboardBindingUpdater : ILeaderboardBindingUpdater
{
    private readonly ITournamentState _state;
    private readonly IBindingEngine _bindingEngine;
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly Settings _settings;

    private Leaderboard Leaderboard => _state.CurrentPreset.Leaderboard;


    public LeaderboardBindingUpdater(ITournamentState state, IBindingEngine bindingEngine, ITournamentPlayerRepository playerRepository,
        ISettingsProvider settingsProvider)
    {
        _state = state;
        _bindingEngine = bindingEngine;
        _playerRepository = playerRepository;
        
        _settings = settingsProvider.Get<Settings>();
    }
    
    public void Publish(int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            LeaderboardEntry entry = Leaderboard.OrderedEntries[i];
            Publish(entry);
        }
    }

    public void Publish(LeaderboardEntry entry)
    {
        //TODO: 0 Dziala, ale nie w pelni z tym, ze nie aktualizuje przy ustaleniu bindingu danych
        // i wyglada jakby nie lapalo publish przy aktualizowaniu w leaderboardzie czasami
        
        int position = entry.Position;
        if (!HaveSpecificPositionBinding(position)) return;

        _bindingEngine.Publish(BindingKey.CreateLeaderboard("points", position), entry.Points);
        _bindingEngine.Publish(BindingKey.CreateLeaderboard("position", position), entry.Position);
        
        IPlayerViewModel? obtainedPlayer = _playerRepository.GetPlayerByUUID(entry.PlayerUUID);
        if (obtainedPlayer is IPlayer player)
        {
            string headUrl = _settings.HeadAPIType.GetHeadURL(player.HeadViewParameter, 180);

            _bindingEngine.Publish(BindingKey.CreateLeaderboard("head", position), headUrl ?? string.Empty);
            _bindingEngine.Publish(BindingKey.CreateLeaderboard("display_name", position), player.DisplayName ?? string.Empty);
            _bindingEngine.Publish(BindingKey.CreateLeaderboard("ign", position), player.InGameName ?? string.Empty);
            _bindingEngine.Publish(BindingKey.CreateLeaderboard("pb", position), player.GetPersonalBest ?? string.Empty);
            _bindingEngine.Publish(BindingKey.CreateLeaderboard("team_name", position), player.TeamName ?? string.Empty);
            _bindingEngine.Publish(BindingKey.CreateLeaderboard("stream_name", position), player.StreamDisplayInfo.Name ?? string.Empty);
            _bindingEngine.Publish(BindingKey.CreateLeaderboard("stream_type", position), player.StreamDisplayInfo.Type);
        }
        
        if (Leaderboard.Rules.Count == 0) return;

        BestMilestoneData? bestMilestoneData = entry.GetBestMilestone(Leaderboard.Rules[0].ChosenAdvancement);
        if (bestMilestoneData is null) return;

        string bestTime = TimeSpan.FromMilliseconds(bestMilestoneData.BestTime).ToFormattedTime();
        string averageTime = TimeSpan.FromMilliseconds(bestMilestoneData.Average).ToFormattedTime();

        _bindingEngine.Publish(BindingKey.CreateLeaderboard("chosen_milestone_best_time", position), bestTime);
        _bindingEngine.Publish(BindingKey.CreateLeaderboard("chosen_milestone_average", position), averageTime);
        _bindingEngine.Publish(BindingKey.CreateLeaderboard("chosen_milestone_amount", position), bestMilestoneData.Amount);
    }

    private bool HaveSpecificPositionBinding(int position)
    {
        foreach (BindingKey key in _bindingEngine.Nodes.Keys)
        {
            if (key is not BindingKeyLeaderboard leaderboardKey) continue;
            if (leaderboardKey.Position != position) continue;

            return true;
        }
        
        return false;
    }
}