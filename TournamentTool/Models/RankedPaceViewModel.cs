using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using TournamentTool.Enums;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Services.Background;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models;

public class RankedPaceViewModel : BaseViewModel, IPlayer, IPace
{
    public class RankedTimelineSplit : BaseViewModel
    {
        public string Name { get; set; } = string.Empty;
        public RankedSplitType Split { get; set; }
        public long Time { get; set; }
    }

    private RankedPace _data;

    private RankedService _service;
    
    public RankedPaceData? PaceData { get; private set; }

    public PlayerViewModel? Player { get; set; }
    public PlayerInventory Inventory { get; set; } = new();

    public ObservableCollection<RankedTimelineSplit> Splits { get; set; } = [];

    private BitmapImage? _headImage;
    public BitmapImage? HeadImage
    {
        get => _headImage;
        set
        {
            _headImage = value;
            OnPropertyChanged(nameof(HeadImage));
        }
    }

    public string DisplayName => Player == null ? InGameName : Player.DisplayName;
    public string GetPersonalBest => Player == null ? "Unk" : Player.GetPersonalBest;
    public string HeadViewParameter => Player == null ? InGameName : Player.HeadViewParameter;
    public string TwitchName => Player == null ? string.Empty : Player.TwitchName;
    public bool IsFromWhitelist => Player != null;

    public string UUID
    {
        get => _data.UUID;
        private set => _data.UUID = value;
    }
    public string InGameName
    {
        get => _data.InGameName;
        set
        {
            _data.InGameName = value;
            OnPropertyChanged(nameof(InGameName));
        }
    }
    public int EloRate
    {
        get => _data.EloRate;
        set
        {
            _data.EloRate = value;
            OnPropertyChanged(nameof(EloRate));
        }
    }
    public List<string> Timelines
    {
        get => _data.Timelines;
        set
        {
            _data.Timelines = value;
            OnPropertyChanged(nameof(Timelines));
        }
    }

    public int _resets;
    public int Resets
    {
        get => _resets;
        set
        {
            _resets = value;
            OnPropertyChanged(nameof(Resets));
        }
    }

    private bool _isUsedInPov;
    public bool IsUsedInPov
    {
        get => _isUsedInPov; 
        set
        {
            _isUsedInPov = value;
            OnPropertyChanged(nameof(IsUsedInPov));
        }
    }

    private bool _isUsedInPreview;
    public bool IsUsedInPreview
    {
        get => _isUsedInPreview;
        set
        {
            _isUsedInPreview = value;
            OnPropertyChanged(nameof(IsUsedInPreview));
        }
    }
    
    private bool _isLive = true;
    public bool IsLive
    {
        get => _isLive;
        set
        {
            _isLive = value;
            OnPropertyChanged(nameof(IsLive));
        }
    }
    
    private float _headImageOpacity;
    public float HeadImageOpacity
    {
        get => _headImageOpacity;
        set
        {
            if (_headImageOpacity == value) return;
            _headImageOpacity = value;
            OnPropertyChanged(nameof(HeadImageOpacity));
        }
    }

    private RankedSplitType _splitType;
    public RankedSplitType SplitType
    {
        get => _splitType;
        set
        {
            if (_splitType == value) return;

            _splitType = value;
            SplitName = value.ToString().Replace('_', ' ').CaptalizeAll();
            OnPropertyChanged(nameof(SplitType));
            OnPropertyChanged(nameof(SplitName));
        }
    }
    public string? SplitName { get; set; }

    private string _lastTimeline = string.Empty;
    public string LastTimeline
    {
        get => _lastTimeline;
        set
        {
            _lastTimeline = value;
            OnPropertyChanged(nameof(LastTimeline));
        }
    }

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

    private long _differenceSplitTimeMiliseconds;
    public long DifferenceSplitTimeMiliseconds
    {
        get => _differenceSplitTimeMiliseconds;
        set
        {
            _differenceSplitTimeMiliseconds = value;
            TimeSpan time = TimeSpan.FromMilliseconds(_differenceSplitTimeMiliseconds);
            SplitDifferenceTime = string.Format("+{0:D2}:{1:D2}", time.Minutes, time.Seconds);
            OnPropertyChanged(nameof(SplitDifferenceTime));
        }
    }
    public string SplitDifferenceTime { get; set; } = "00:00";


    public RankedPaceViewModel(RankedService service, RankedPace data)
    {
        _service = service;
        _data = data;
    }
    public void Initialize(RankedPaceData data)
    {
        PaceData = data;
        Inventory.DisplayItems = true;
        Splits.Add(new RankedTimelineSplit { Name = "Start", Split = RankedSplitType.Start, Time = 0 });
        
        Update(data);
    }

    /// <summary>
    /// trzeba zrobic prostsze aktualizowanie timelineow etap po etapie, a nie cala na raz
    /// </summary>
    public void Update(RankedPaceData data)
    {
        PaceData = data;
        
        IsLive = Player != null && !Player.StreamData.AreBothNullOrEmpty();
        UpdateHeadImage();
        
        if (Resets != data.Resets)
        {
            Timelines.Clear();
            Splits.Clear();
            Splits.Add(new RankedTimelineSplit() { Name = "Start", Split = RankedSplitType.Start, Time = 0 });

            //TODO: tu moze lepiej zgarnac timeline resetu i pobrac czas zeby na starcie nie bylo 00:00 tylko czas resetu z racji i tak rta czasu reszty splitow 
            RankedTimelineSplit last = Splits[^1];
            SplitType = last.Split;
            CurrentSplitTimeMiliseconds = last.Time;
            LastTimeline = string.Empty;
        }
        if (data.Timelines.Count == 0) return;

        Resets = data.Resets;

        for (int i = 0; i < data.Timelines.Count; i++)
        {
            var current = data.Timelines[i];
            if (Timelines.Count > i && current.Type.Equals(Timelines[i])) continue;

            Timelines.Add(current.Type);
        }

        UpdateSplits(data.Timelines);
        UpdateInventory(data.Inventory);

        RankedTimelineSplit lastSplit = Splits[^1];
        SplitType = lastSplit.Split;
        CurrentSplitTimeMiliseconds = lastSplit.Time;

        if (Timelines.Count == 0) return;
        LastTimeline = Timelines[^1].CaptalizeAll();
    }

    private void UpdateSplits(List<RankedTimeline> timelines)
    {
        for (int i = 0; i < timelines.Count; i++)
        {
            var timeline = timelines[i];
            bool wasFound = false;

            for (int j = 0; j < Splits.Count; j++)
            {
                var current = Splits[j];
                if (!current.Name.Equals(timeline.Type)) continue;
                
                wasFound = true;
                break;
            }
            if (wasFound) continue;

            RankedTimelineSplit? newSplit = null;
            if (Enum.TryParse(typeof(RankedSplitType), timeline.Type, true, out var split))
            {
                newSplit = new RankedTimelineSplit { Name = timeline.Type, Split = (RankedSplitType)split, Time = timeline.Time };
            }
            else if ((timeline.Type.Equals("find_bastion") || timeline.Type.Equals("find_fortress")) && Splits.Count > 0)
            {
                var splitType = RankedSplitType.structure_2;

                if (Splits[^1].Name.Equals("enter_the_nether"))
                    splitType = RankedSplitType.structure_1;

                newSplit = new RankedTimelineSplit { Name = timeline.Type, Split = splitType, Time = timeline.Time };
            }

            if (newSplit == null) continue;

            ValidateBestSplit(newSplit);
            Splits.Add(newSplit);
        }
    }
    private void UpdateInventory(RankedInventory inventory)
    {
        Inventory.BlazeRodsCount = inventory.BlazeRod;
        Inventory.ObsidianCount = inventory.Obsidian;
        Inventory.BedsCount = inventory.WhiteBed;
        Inventory.EnderEyeCount = inventory.EnderEye;
        Inventory.PearlsCount = inventory.EnderPearl;
    }

    private void UpdateHeadImage()
    {
        HeadImageOpacity = HeadImage != null && IsLive ? 1f : .25f;
        if (HeadImage != null) return;

        if (Player == null)
        {
            string url = $"https://minotar.net/helm/{InGameName}/8.png";
            Task.Run(async () =>
            {
                HeadImage = await Helper.LoadImageFromUrlAsync(url);
            });
        }
        else
        {
            HeadImage = Player!.Image;
        }
    }

    private void ValidateBestSplit(RankedTimelineSplit newSplit)
    {
        RankedBestSplit bestSplit = _service.GetBestSplit(newSplit.Split);
        
        if(string.IsNullOrEmpty(bestSplit.PlayerName))
        {
            bestSplit.PlayerName = InGameName;
            bestSplit.Time = newSplit.Time;
        }
        DifferenceSplitTimeMiliseconds = newSplit.Time - bestSplit.Time;
        if (DifferenceSplitTimeMiliseconds < 0) DifferenceSplitTimeMiliseconds = 0;
    }
    
    public RankedTimelineSplit GetLastSplit()
    {
        return GetSplit(1)!;
    }
    public RankedTimelineSplit? GetSplit(int indexFromEnd)
    {
        if (indexFromEnd > Splits.Count) return null;
        var split = Splits[^indexFromEnd];
        return new RankedTimelineSplit()
        {
            Name = split.Name,
            Split = split.Split,
            Time = split.Time
        };
    }
}
