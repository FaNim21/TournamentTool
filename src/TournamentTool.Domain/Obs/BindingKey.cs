using System.Text.Json.Serialization;

namespace TournamentTool.Domain.Obs;

public abstract record NameableSchema
{
    public abstract string Name { get; }
}

public abstract record BindingSubSchema(string Field) : NameableSchema
{
    public static BindingWhitelistSubSchema CreateWhitelist(string field) => new(field.ToLower());
    public static BindingWhitelistSubSchema GetWhitelistSubSchema() => new(string.Empty);
}
public sealed record BindingWhitelistSubSchema(string Field) : BindingSubSchema(Field)
{
    public override string Name => "Whitelist";
}

public abstract record BindingSchema(string Field) : NameableSchema
{
    public static BindingPOVSchema CreatePOV(string field) => new(field.ToLower());
    public static BindingPOVSchema GetPOV() => new(string.Empty);
    
    public static BindingRankedManagementSchema CreateRankedManagement(string field) => new(field.ToLower());
    public static BindingRankedManagementSchema GetRankedManagement() => new(string.Empty);
    
    public static BindingLeaderboardSchema CreateLeaderboard(string field) => new(field.ToLower());
    public static BindingLeaderboardSchema GetLeaderboard() => new(string.Empty);
}

public sealed record BindingPOVSchema(string Field) : BindingSchema(Field)
{
    public override string Name => "POV";
}
public sealed record BindingRankedManagementSchema(string Field) : BindingSchema(Field)
{
    public override string Name => "Ranked_management";
}
public sealed record BindingLeaderboardSchema(string Field) : BindingSchema(Field)
{
    public override string Name => "Leaderboard";
}

[JsonDerivedType(typeof(BindingKeyLeaderboard), typeDiscriminator: "leaderboard")]
[JsonDerivedType(typeof(BindingKeyRankedManagement), typeDiscriminator: "ranked_management")]
[JsonDerivedType(typeof(BindingKeyPOV), typeDiscriminator: "pov")]
[JsonDerivedType(typeof(BindingKeyEmpty), typeDiscriminator: "empty")]
public abstract record BindingKey
{
    public static BindingKeyEmpty CreateEmpty() => new();
    public static BindingKeyPOV CreatePov(string field, string povName) => new(field.ToLower(), povName.ToLower());
    public static BindingKeyRankedManagement CreateRankedManagement(string field) => new(field.ToLower());
    public static BindingKeyLeaderboard CreateLeaderboard(string field, int position) => new(field.ToLower(), position);
    
    public abstract bool IsEmpty();
}

public sealed record BindingKeyEmpty : BindingKey
{
    public override bool IsEmpty() => true;
}
public sealed record BindingKeyPOV(string Field, string PovName) : BindingKey
{
    public override bool IsEmpty() => string.IsNullOrEmpty(Field) && string.IsNullOrEmpty(PovName);
}
public sealed record BindingKeyRankedManagement(string Field) : BindingKey
{
    public override bool IsEmpty() => string.IsNullOrEmpty(Field);
}
public sealed record BindingKeyLeaderboard(string Field, int Position) : BindingKey
{
    public override bool IsEmpty() => string.IsNullOrEmpty(Field);
}