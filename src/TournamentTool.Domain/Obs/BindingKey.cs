namespace TournamentTool.Domain.Obs;

public sealed record BindingSchema(string Source, string Field, bool haveName, bool haveIndex)
{
    public static BindingSchema New(string Source, string Field, bool haveName = false, bool haveIndex = false) => new BindingSchema(Source, Field, haveName, haveIndex);
    
    public static BindingSchema CreateEmpty() => new BindingSchema(string.Empty, string.Empty, false, false);
}

public sealed record BindingKey(string Source, string Field, string? Pov = null, int? Index = null)
{
    public static BindingKey New(string Source, string Field, string? Pov = null, int? Index = null) => new BindingKey(Source, Field, Pov, Index);
    
    public static BindingKey CreateEmpty() => new BindingKey(string.Empty, string.Empty, null, null);
}

public static class BindingKeyHelper
{
    public static bool IsEmpty(this BindingKey bindingKey)
    {
        return bindingKey == null || 
               (string.IsNullOrEmpty(bindingKey.Source) &&
                string.IsNullOrEmpty(bindingKey.Field) &&
                bindingKey.Pov == null &&
                bindingKey.Index == null);
    }
}

