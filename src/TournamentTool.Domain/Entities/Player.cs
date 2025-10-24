using System.Text.Json.Serialization;
using TournamentTool.Domain.Enums;

namespace TournamentTool.Domain.Entities;

public readonly struct ResponseMojangProfileAPI
{
    [JsonPropertyName("id")]
    public string UUID { get; init; }
    
    [JsonPropertyName("name")]
    public string InGameName { get; init; } 
}

public interface IStreamServiceData
{
    public string ID { get; }
    public StreamType ServiceType { get; }

    StreamDisplayInfo GetDisplayInfo();
}
public class TwitchStreamData : IStreamServiceData
{
    public string ID { get; set; } = string.Empty;
    public StreamType ServiceType => StreamType.twitch;

    public string BroadcasterID { get; set; } = string.Empty;
    public string UserLogin { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ViewerCount { get; set; }
    public DateTime StartedAt { get; set; }
    public string Language { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "offline";
    public string GameName { get; set; } = string.Empty;
    public bool GameNameVisibility { get; set; }
    
    public StreamDisplayInfo GetDisplayInfo()
    {
        return new StreamDisplayInfo(UserLogin, ServiceType);
    }
}

public class StreamData
{
    //TODO: 1 Zrobic to generic zeby byl uwzgledniony interface modelu co sprawi ze to bedzie modularne, ale nie robie tego
    // piszac ten komentarz za racji refactora calej struktury projektu i pozostalych mi 966 bledow xD
    [JsonIgnore] public IStreamServiceData? StreamService { get; set; }

    public string Main { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public string Other { get; set; } = string.Empty;
    public StreamType OtherType { get; set; } = StreamType.kick;
    
    
    public bool ExistName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        
        return Main.Equals(name, StringComparison.OrdinalIgnoreCase) || Alt.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
    
    public StreamDisplayInfo GetCorrectStream()
    {
        if (!string.IsNullOrEmpty(Other))
            return new StreamDisplayInfo(Other, OtherType);
        if (string.IsNullOrEmpty(Main))
            return new StreamDisplayInfo(Alt, StreamType.twitch);
        return new StreamDisplayInfo(Main, StreamType.twitch);
    }
    
    public bool IsMainEmpty()
    {
        return string.IsNullOrEmpty(Main);
    }
    public bool IsAltEmpty()
    {
        return string.IsNullOrEmpty(Alt);
    }
    public bool IsOtherEmpty()
    {
        return string.IsNullOrEmpty(Other);
    }
    public bool AreBothNullOrEmpty()
    {
        return IsMainEmpty() && IsAltEmpty();
    }

    public StreamDisplayInfo GetStreamDisplayInfo()
    {
        if (StreamService == null) return new StreamDisplayInfo(string.Empty, StreamType.twitch);
        if (string.IsNullOrEmpty(StreamService.ID)) return GetCorrectStream();
        
        return StreamService.GetDisplayInfo();
    }
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

    [JsonIgnore] public bool IsUsedInPov { get; set; }
    [JsonIgnore] public bool IsUsedInPreview { get; set; }
}
