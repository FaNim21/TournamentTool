using TournamentTool.Enums;

namespace TournamentTool.Utils.Parsers;

public readonly record struct RunMilestoneData(string Name, RunMilestone Milestone);

public static class RunMilestoneParser
{
    private static readonly Dictionary<string, RunMilestoneData> _cachedMilestones = new();
    private static readonly RunMilestoneData DefaultValue;


    static RunMilestoneParser()
    {
        DefaultValue = new RunMilestoneData("none", RunMilestone.None);
        foreach (RunMilestone run in Enum.GetValues<RunMilestone>())
        {
            var attribute = run.GetDisplay();
            if (attribute == null) continue;

            var name = attribute.ShortName;
            var milestone = EnumExtensions.FromDescription<RunMilestone>(attribute.Description!);
            
            var result = new RunMilestoneData(name!, milestone);
            _cachedMilestones[attribute.Description!] = result;
        }
    }
    
    public static bool TryParse(string? type, out RunMilestoneData result)
    {
        if (!string.IsNullOrWhiteSpace(type)) return _cachedMilestones.TryGetValue(type, out result);

        result = DefaultValue;
        return false;
    }
    public static RunMilestoneData Parse(string type)
    {
        return TryParse(type, out var data) ? data : DefaultValue;
    }
}