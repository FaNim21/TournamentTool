namespace TournamentTool.Domain.Obs;

public sealed record BindingSchema(string Source, string Field, bool haveName, bool haveIndex)
{
    //TODO: Zrobic BindingScheme jako abstract i wtedy dzieci beda definiowac binding scheme zaleznie od pov, leaderboard, itp itd w przyszlosci
    
    public static BindingSchema New(string Source, string Field, bool haveName = false, bool haveIndex = false) => new(Source, Field.ToLower(), haveName, haveIndex);
    
    public static BindingSchema Empty() => new(string.Empty, string.Empty, false, false);
}

public sealed record BindingKey(string Source, string Field, string? Name = null, int? Index = null)
{
    public static BindingKey New(string Source, string Field, string? Pov = null, int? Index = null) => new(Source, Field.ToLower(), Pov?.ToLower(), Index);
    
    public static BindingKey Empty() => new(string.Empty, string.Empty);
}

public static class BindingKeyHelper
{
    public static bool IsEmpty(this BindingKey bindingKey)
    {
        return bindingKey == null || 
               (string.IsNullOrEmpty(bindingKey.Source) &&
                string.IsNullOrEmpty(bindingKey.Field) &&
                bindingKey.Name == null &&
                bindingKey.Index == null);
    }
}