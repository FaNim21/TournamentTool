using System.Collections.ObjectModel;
using System.Data;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public enum RankedSplitType
{
    none,
    enter_the_nether,
    structure1,  // find_bastion
    structure2,  // find_fortress
    blind_travel,
    follow_ender_eye,
    enter_the_end,
    kill_dragon,
    complete,
}

public class RankedPace : BaseViewModel, IPlayer, IPace
{
    private ControllerViewModel Controller { get; set; }

    public Player? Player { get; set; }
    public PlayerInventory Inventory { get; set; } = new();

    public ObservableCollection<RankedSplitType> Splits { get; set; } = [];

    private string _inGameName { get; set; }
    public string InGameName
    {
        get => _inGameName;
        set
        {
            _inGameName = value;
            OnPropertyChanged(nameof(InGameName));
        }
    }

    private int _eloRate { get; set; }
    public int EloRate
    {
        get => _eloRate;
        set
        {
            _eloRate = value;
            OnPropertyChanged(nameof(EloRate));
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

    private List<string> _timelines;
    public List<string> Timelines
    {
        get => _timelines;
        set
        {
            _timelines = value;
            OnPropertyChanged(nameof(Timelines));
        }
    }

    private string _lastTimeline;
    public string LastTimeline
    {
        get => _lastTimeline;
        set
        {
            _lastTimeline = value;
            OnPropertyChanged(nameof(LastTimeline));
        }
    }


    public RankedPace(ControllerViewModel controller)
    {
        Controller = controller;
    }

    public void Update(RankedPaceData data)
    {
        /*Timelines.Clear();
        for (int i = 0; i < data.Timelines.Count; i++)
        {

        }*/

        Inventory.BlazeRodsCount = data.Inventory.BlazeRod;
        Inventory.ObsidianCount = data.Inventory.Obsidian;
        Inventory.BedsCount = data.Inventory.WhiteBed;
        Inventory.EnderEyeCount = data.Inventory.EnderEye;
        Inventory.PearlsCount = data.Inventory.EnderPearl;

        if (data.Timelines.Count == 0) return;
        LastTimeline = data.Timelines[^1].Type.ToString();
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
