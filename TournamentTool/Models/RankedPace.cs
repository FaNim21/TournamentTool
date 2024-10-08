﻿using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public enum RankedSplitType
{
    none,
    Start,
    enter_the_nether,
    structure_1,  // find_bastion
    structure_2,  // find_fortress
    blind_travel,
    follow_ender_eye,
    enter_the_end,
    kill_dragon,
    complete,
}

public class RankedPace : BaseViewModel, IPlayer, IPace
{
    public class RankedTimelineSplit : BaseViewModel
    {
        public string Name { get; set; } = string.Empty;
        public RankedSplitType Split { get; set; }
        public long Time { get; set; }
    }

    public ControllerViewModel Controller { get; set; }
    private RankedPacePanel RankedPacePanel { get; set; }

    public Player? Player { get; set; }
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

    private string _inGameName { get; set; } = string.Empty;
    public string InGameName
    {
        get => _inGameName;
        set
        {
            _inGameName = value;
            OnPropertyChanged(nameof(InGameName));
        }
    }

    private int _eloRate { get; set; } = -1;
    public int EloRate
    {
        get => _eloRate;
        set
        {
            _eloRate = value;
            OnPropertyChanged(nameof(EloRate));
        }
    }

    private int _resets { get; set; }
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

    private List<string> _timelines = [];
    public List<string> Timelines
    {
        get => _timelines;
        set
        {
            _timelines = value;
            OnPropertyChanged(nameof(Timelines));
        }
    }

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


    public RankedPace(ControllerViewModel controller, RankedPacePanel rankedPacePanel)
    {
        Controller = controller;
        RankedPacePanel = rankedPacePanel;
    }
    public void Initialize(RankedPlayer player)
    {
        InGameName = player.NickName;
        EloRate = player.EloRate ?? -1;

        Splits.Add(new RankedTimelineSplit() { Name = "Start", Split = RankedSplitType.Start, Time = 0 });
        UpdateHeadImage();
    }

    public void Update(RankedPaceData data)
    {
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
                if (current.Name.Equals(timeline.Type))
                {
                    wasFound = true;
                    break;
                }
            }
            if (wasFound) continue;

            RankedTimelineSplit? newSplit = null;
            if (Enum.TryParse(typeof(RankedSplitType), timeline.Type, true, out var split))
            {
                newSplit = new RankedTimelineSplit() { Name = timeline.Type, Split = (RankedSplitType)split, Time = timeline.Time };
            }
            else if ((timeline.Type.Equals("find_bastion") || timeline.Type.Equals("find_fortress")) && Splits.Count > 0)
            {
                var splitType = RankedSplitType.structure_2;

                if (Splits[^1].Name.Equals("enter_the_nether"))
                    splitType = RankedSplitType.structure_1;

                newSplit = new RankedTimelineSplit() { Name = timeline.Type, Split = splitType, Time = timeline.Time };
            }

            if (newSplit == null) continue;

            //TODO: to powinno byc zapisywane inaczej i forowane po wszystkich dla wiekszej pewnosci? poniewaz po resecie aplikacji
            //ta metoda jest obecnie najwydajniejsza, ale psuje sie po restarcie apki z racji kolejnosci czytania danych z pliku json ktorego sie nie da zmienic
            RankedBestSplit bestSplit = RankedPacePanel.GetBestSplit(newSplit.Split);
            if(string.IsNullOrEmpty(bestSplit.PlayerName))
            {
                bestSplit.PlayerName = InGameName;
                bestSplit.Time = newSplit.Time;
            }
            DifferenceSplitTimeMiliseconds = newSplit.Time - bestSplit.Time;
            if (DifferenceSplitTimeMiliseconds < 0) DifferenceSplitTimeMiliseconds = 0;

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
        if (HeadImage != null) return;

        if (Player == null)
        {
            string url = $"https://minotar.net/helm/{InGameName}/180.png";
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

    public string GetDisplayName()
    {
        return Player == null ? InGameName : Player.GetDisplayName();
    }
    public string GetPersonalBest()
    {
        return Player == null ? "Unk" : Player.GetPersonalBest();
    }
    public string GetTwitchName()
    {
        return Player == null ? string.Empty : Player.GetTwitchName();
    }
    public string GetHeadViewParametr()
    {
        return Player == null ? InGameName : Player.GetHeadViewParametr();
    }
    public bool IsFromWhiteList()
    {
        return Player != null;
    }
}
