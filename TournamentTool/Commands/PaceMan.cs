using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands;

public class PaceMan : BaseViewModel
{
    [JsonPropertyName("user")]
    public PaceManUser User { get; set; } = new();

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("eventList")]
    public List<PaceEventList> Splits { get; set; } = [];

    public BitmapImage? Image { get; set; }

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


    public void UpdateImage(BitmapImage image)
    {
        Image = image;
        OnPropertyChanged(nameof(Image));
    }
    public void UpdateTime(string splitName)
    {
        SplitName = splitName;
        OnPropertyChanged(nameof(SplitName));
        PaceEventList? currentPace = Splits.LastOrDefault();
        if (currentPace == null) return;

        CurrentSplitTimeMiliseconds = currentPace.IGT;
    }
}

public class PaceEventList
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
