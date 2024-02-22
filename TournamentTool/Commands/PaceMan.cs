using System;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;
using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands;

public class PaceMan : BaseViewModel
{
    [JsonPropertyName("user")]
    public PaceManUser User { get; set; } = new();

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("eventList")]
    public List<PaceSplitsList> Splits { get; set; } = [];

    [JsonPropertyName("lastUpdated")]
    public long LastUpdate { get; set; }

    [JsonIgnore]
    public Player? Player { get; set; }

    public string? SplitName { get; set; }

    private long _currentSplitTimeMiliseconds;
    public long CurrentSplitTimeMiliseconds
    {
        get => _currentSplitTimeMiliseconds;
        set
        {
            _currentSplitTimeMiliseconds = value;
            TimeSpan time = TimeSpan.FromMilliseconds(CurrentSplitTimeMiliseconds);
            CurrentSplitTime = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
            OnPropertyChanged(nameof(CurrentSplitTime));
        }
    }
    public string CurrentSplitTime { get; set; } = "00:00";

    public long IGTTimeMiliseconds { get; set; }


    public void UpdateTime(string splitName)
    {
        SplitName = splitName;
        OnPropertyChanged(nameof(SplitName));
        PaceSplitsList? currentPace = Splits.LastOrDefault();
        if (currentPace == null) return;

        //TODO: 0 zrobic aktualizowanie IGT, a potem juz update'owac pacemana
        /*TimeSpan now = ;
        TimeSpan last = TimeSpan.FromMilliseconds(timeMiliseconds);*/
        CurrentSplitTimeMiliseconds = currentPace.IGT;
    }
}

public class PaceSplitsList
{
    [JsonPropertyName("eventId")]
    public string? SplitName { get; set; }

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
