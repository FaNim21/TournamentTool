using TournamentTool.Core.Extensions;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Obs;
using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Obs.Binding;

public sealed class BindingDataGetter : IBindingDataGetter
{
    private readonly ITournamentState _tournamentState;
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly ISceneItemGetter _sceneItemGetter;
    private readonly Settings _settings;

    private Tournament Preset => _tournamentState.CurrentPreset;


    public BindingDataGetter(ITournamentState tournamentState, ITournamentPlayerRepository playerRepository, ISettingsProvider settingsProvider,
        ISceneItemGetter sceneItemGetter)
    {
        _tournamentState = tournamentState;
        _playerRepository = playerRepository;
        _sceneItemGetter = sceneItemGetter;
        _settings = settingsProvider.Get<Settings>();
    }

    public string GetData(BindingKey key)
    {
        return key switch
        {
            BindingKeyLeaderboard leaderboardKey => GetLeaderboard(leaderboardKey),
            BindingKeyPOV povKey => GetPov(povKey),
            BindingKeyRankedManagement rankedManagementKey => GetRankedManagement(rankedManagementKey),
            _ => string.Empty
        };
    }
    
    private string GetLeaderboard(BindingKeyLeaderboard leaderboardKey)
    {
        LeaderboardEntry? entry = Preset.Leaderboard.OrderedEntries.Count > leaderboardKey.Position
            ? Preset.Leaderboard.OrderedEntries[leaderboardKey.Position - 1] : null;
        if (entry is null) return string.Empty;
        
        LeaderboardRule? chosenRule = Preset.Leaderboard.Rules.Count > 0 ? Preset.Leaderboard.Rules[0] : null;
        RunMilestone chosenAdvancement = chosenRule?.ChosenAdvancement ?? RunMilestone.None;
        
        BestMilestoneData? bestMilestoneData = entry.GetBestMilestone(chosenAdvancement);

        object? outputData = leaderboardKey.Field switch
        {
            "points" => entry.Points.ToString(),
            "position" => entry.Position.ToString(),
            "chosen_milestone_best_time" => bestMilestoneData is { } ? TimeSpan.FromMilliseconds(bestMilestoneData.BestTime).ToFormattedTime() : string.Empty,
            "chosen_milestone_average" => bestMilestoneData is { } ? TimeSpan.FromMilliseconds(bestMilestoneData.Average).ToFormattedTime() : string.Empty,
            "chosen_milestone_amount" => bestMilestoneData is { } ? TimeSpan.FromMilliseconds(bestMilestoneData.Amount) : string.Empty,
            "chosen_milestone_rule_name" => chosenRule?.Name ?? string.Empty,
            _ => null
        };

        if (outputData is { }) return ConvertToOutput(outputData);
        
        IPlayerViewModel? player = _playerRepository.GetPlayerByUUID(entry.PlayerUUID);
        outputData = GetPlayer(leaderboardKey.Field, player as IPlayer);

        return ConvertToOutput(outputData);
    }
    
    private string GetRankedManagement(BindingKeyRankedManagement rankedManagementKey)
    {
        if (Preset.ManagementData is not RankedManagementData rankedManagementData) return string.Empty;
        
        object? outputData = rankedManagementKey.Field switch
        {
            nameof(RankedManagementData.CustomText) => rankedManagementData.CustomText,
            nameof(RankedManagementData.Rounds) => rankedManagementData.Rounds,
            nameof(RankedManagementData.Completions) => rankedManagementData.Completions,
            nameof(RankedManagementData.Players) => rankedManagementData.Players,
            _ => string.Empty
        };
        
        return ConvertToOutput(outputData);
    }

    private string GetPov(BindingKeyPOV povKey)
    {
        object? outputData = povKey.Field switch
        {
            "xd" => "easter egg",
            _ => null
        };

        if (outputData is { }) return ConvertToOutput(outputData);

        IPointOfView? pov = _sceneItemGetter.GetPointOfView(povKey.PovName);
        if (pov is null) return string.Empty;

        outputData = GetPlayer(povKey.Field, pov.Player);

        return ConvertToOutput(outputData);
    }
    
    private object? GetPlayer(string field, IPlayer? player)
    {
        if (player is null) return string.Empty;
        
        object? outputData = field switch
        {
            "head" => _settings.HeadAPIType.GetHeadURL(player.HeadViewParameter, 180),
            "display_name" => player.DisplayName,
            "ign" => player.InGameName,
            "pb" => player.GetPersonalBest,
            "team_name" => player.TeamName,
            "stream_name" => player.StreamDisplayInfo.Name,
            "stream_type" => player.StreamDisplayInfo.Type,
            _ => string.Empty
        };
        
        return outputData;
    }
    
    private static string ConvertToOutput(object? outputData)
    {
        if (outputData is string text) return text;
        return outputData?.ToString() ?? string.Empty;
    }
}