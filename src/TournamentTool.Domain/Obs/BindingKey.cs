using System.Text.Json.Serialization;

namespace TournamentTool.Domain.Obs;

//TODO: 3 Zrobic BindingScheme jako abstract i wtedy dzieci beda definiowac binding scheme zaleznie od pov, leaderboard, itp itd w przyszlosci
// to trzeba jednak zrobic pozniej przez to jak czytane jest to w edytorze itp itd

//Punkt jest taki, ze jak zrobie klasy zaleznie od typu schema to moge wtedy ewentualnie ustalac mozliwosci w xaml ze strony typu zaladowanej schemy czy bindingu
// poniewaz i tak wtedy scene item type mialby support ogolnie do bindingu, a reszta by sie przelaczala ze wzgledu na typ bindingu

public abstract record BindingSchema
{
    public abstract string Name { get; }
    
    public static BindingPOVSchema CreatePOV(string field) => new(field.ToLower());
    public static BindingRankedManagement CreateRankedManagement(string field) => new(field.ToLower());
}

public sealed record BindingPOVSchema(string Field) : BindingSchema
{
    public override string Name => "POV";
}
public sealed record BindingRankedManagement(string Field) : BindingSchema
{
    public override string Name => "Ranked_management";
}

[JsonDerivedType(typeof(BindingKeyRankedManagement), typeDiscriminator: "ranked_management")]
[JsonDerivedType(typeof(BindingKeyPOV), typeDiscriminator: "pov")]
[JsonDerivedType(typeof(BindingKeyEmpty), typeDiscriminator: "empty")]
public abstract record BindingKey
{
    public static BindingKeyPOV CreatePov(string field, string povName) => new(field.ToLower(), povName.ToLower());
    public static BindingKeyRankedManagement CreateRankedManagement(string field) => new(field.ToLower());
    public static BindingKeyEmpty CreateEmpty() => new();
    
    public abstract bool IsEmpty();
}

public sealed record BindingKeyPOV(string Field, string PovName) : BindingKey
{
    public override bool IsEmpty() => string.IsNullOrEmpty(Field) && string.IsNullOrEmpty(PovName);
}
public sealed record BindingKeyRankedManagement(string Field) : BindingKey
{
    public override bool IsEmpty() => string.IsNullOrEmpty(Field);
}
public sealed record BindingKeyEmpty : BindingKey
{
    public override bool IsEmpty() => true;
}

public static class BindingKeyHelper
{
    public static BindingSchema? GetSchema(this BindingKey bindingKey) =>
        bindingKey switch
        {
            BindingKeyPOV pov => BindingSchema.CreatePOV(pov.Field),
            BindingKeyRankedManagement rankedManagement => BindingSchema.CreateRankedManagement(rankedManagement.Field),
            _ => null
        };
}

// OLDDD
/*public sealed record BindingSchema(string Source, string Field, bool haveName, bool haveIndex)
{
    public static BindingSchema New(string Source, string Field, bool haveName = false, bool haveIndex = false) => new(Source, Field.ToLower(), haveName, haveIndex);
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
}*/