namespace TournamentTool.Models.Ranking;

public readonly record struct SubRuleWinnerData(string UUID, int time);

public class LeaderboardSubRule
{
    public string LuaPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Time { get; set; }
    public int BasePoints { get; set; }
    public int MaxWinners { get; set; }
    public bool Repeatable { get; set; }

    public List<SubRuleWinnerData> winners = [];
    
    
    public bool Evaluate(LeaderboardPlayerEvaluateData data)
    {
        return data.MainSplit.Time <= Time;
    }
}