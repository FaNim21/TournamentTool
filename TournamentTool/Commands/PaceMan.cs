using System.Text.Json.Serialization;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands;

public class PaceMan : BaseViewModel, ITwitchPovInformation
{
    [JsonPropertyName("user")]
    public PaceManUser User { get; set; } = new();

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("eventList")]
    public List<PaceSplitsList> Splits { get; set; } = [];

    [JsonPropertyName("lastUpdated")]
    public long LastUpdate { get; set; }

    public Player? Player { get; set; }

    public string? SplitName { get; set; }

    private long _currentSplitTimeMiliseconds;
    public long CurrentSplitTimeMiliseconds
    {
        get => _currentSplitTimeMiliseconds;
        set
        {
            _currentSplitTimeMiliseconds = value;
            TimeSpan time = TimeSpan.FromMilliseconds(_currentSplitTimeMiliseconds);
            CurrentSplitTime = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
            OnPropertyChanged(nameof(CurrentSplitTime));
        }
    }
    public string CurrentSplitTime { get; set; } = "00:00";

    private long _igtTimeMiliseconds;
    public long IGTTimeMiliseconds
    {
        get => _igtTimeMiliseconds;
        set
        {
            _igtTimeMiliseconds = value;
            TimeSpan time = TimeSpan.FromMilliseconds(_igtTimeMiliseconds);
            IGTTime = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
            OnPropertyChanged(nameof(IGTTime));
        }
    }
    public string IGTTime { get; set; } = "00:00";


    public void Update(PaceMan paceman)
    {
        User = paceman.User;
        Splits = paceman.Splits;
        LastUpdate = paceman.LastUpdate;
        PaceSplitsList? lastSplit = Splits.LastOrDefault();
        if (lastSplit == null) return;
        UpdateTime(Helper.GetSplitShortcut(lastSplit.SplitName));
    }
    public void UpdateTime(string splitName)
    {
        SplitName = splitName + ":";
        OnPropertyChanged(nameof(SplitName));
        PaceSplitsList? currentPace = Splits.LastOrDefault();
        if (currentPace == null) return;

        CurrentSplitTimeMiliseconds = currentPace.IGT;
        IGTTimeMiliseconds = ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - LastUpdate) + currentPace.IGT;
    }

    public string GetDisplayName()
    {
        return Nickname;
    }
    public string GetTwitchName()
    {
        return User.TwitchName;
    }
    public string GetHeadViewParametr()
    {
        return User.UUID;
    }
}

public class PaceSplitsList
{
    [JsonPropertyName("eventId")]
    public string SplitName { get; set; } = string.Empty;

    [JsonPropertyName("rta")]
    public long RTA { get; set; }

    [JsonPropertyName("igt")]
    public long IGT { get; set; }
}

public class PaceManUser
{
    [JsonPropertyName("uuid")]
    public string UUID { get; set; }

    [JsonPropertyName("liveAccount")]
    public string TwitchName { get; set; }
}
