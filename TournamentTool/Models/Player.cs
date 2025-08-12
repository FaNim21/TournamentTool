using System.Text.Json.Serialization;
using TournamentTool.Enums;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public readonly struct ResponseMojangProfileAPI
{
    [JsonPropertyName("id")]
    public string UUID { get; init; }
    
    [JsonPropertyName("name")]
    public string InGameName { get; init; } 
}

public class StreamData : BaseViewModel
{
    public string Main { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public string Other { get; set; } = string.Empty;
    public StreamType OtherType { get; set; } = StreamType.kick;
}

public class Player
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string UUID { get; set; } = string.Empty;
    public byte[]? ImageStream { get; set; }
    public string? Name { get; set; } = string.Empty;
    public StreamData StreamData { get; init; } = new();
    public string? InGameName { get; set; } = string.Empty;
    public string PersonalBest { get; set; } = string.Empty;
    public string? TeamName { get; set; } = string.Empty;
}
