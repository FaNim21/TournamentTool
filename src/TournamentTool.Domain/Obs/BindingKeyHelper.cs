namespace TournamentTool.Domain.Obs;

public static class BindingKeyHelper
{
    public static BindingSchema? GetSchema(this BindingKey bindingKey) =>
        bindingKey switch
        {
            BindingKeyPOV pov => BindingSchema.CreatePOV(pov.Field),
            BindingKeyRankedManagement rankedManagement => BindingSchema.CreateRankedManagement(rankedManagement.Field),
            BindingKeyLeaderboard leaderboard => BindingSchema.CreateLeaderboard(leaderboard.Field),
            _ => null
        };
}