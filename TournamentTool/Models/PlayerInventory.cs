using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class PlayerInventory
{
    public bool DisplayItems { get; set; }
    public int PearlsCount { get; set; }
    public int BlazeRodsCount { get; set; }
    public int BedsCount { get; set; }
    public int ObsidianCount { get; set; }
    public int EnderEyeCount { get; set; }
    
    public bool DisplayItemsInPace()
    {
        if (DisplayItems) return false;
        return true;
    }
    public void Clear()
    {
        BedsCount = 0;
        BlazeRodsCount = 0;
        BedsCount = 0;
        ObsidianCount = 0;
        EnderEyeCount = 0;
    }
}

public class PlayerInventoryViewModel : BaseViewModel
{
    private PlayerInventory _data;
    
    private bool _displayItems { get; set; } = false;
    public bool DisplayItems
    {
        get => _displayItems;
        set
        {
            if (_displayItems == value) return;
            _displayItems = value;
            OnPropertyChanged(nameof(DisplayItems));
        }
    }

    private int _pearlsCount { get; set; }
    public int PearlsCount
    {
        get => _pearlsCount;
        set
        {
            if (_pearlsCount == value) return;
            _pearlsCount = value;
            OnPropertyChanged(nameof(PearlsCount));
        }
    }

    private int _blazeRodsCount { get; set; }
    public int BlazeRodsCount
    {
        get => _blazeRodsCount;
        set
        {
            if (_blazeRodsCount == value) return;
            _blazeRodsCount = value;
            OnPropertyChanged(nameof(BlazeRodsCount));
        }
    }

    private int _bedsCount { get; set; }
    public int BedsCount
    {
        get => _bedsCount;
        set
        {
            if (_bedsCount == value) return;
            _bedsCount = value;
            OnPropertyChanged(nameof(BedsCount));
        }
    }

    private int _obsidianCount { get; set; }
    public int ObsidianCount
    {
        get => _obsidianCount;
        set
        {
            if (_obsidianCount == value) return;
            _obsidianCount = value;
            OnPropertyChanged(nameof(ObsidianCount));
        }
    }

    private int _enderEyeCount { get; set; }
    public int EnderEyeCount
    {
        get => _enderEyeCount;
        set
        {
            if (_enderEyeCount == value) return;
            _enderEyeCount = value;
            OnPropertyChanged(nameof(EnderEyeCount));
        }
    }


    public PlayerInventoryViewModel(PlayerInventory data)
    {
        _data = data;
    }

    public void Update()
    {
        DisplayItems = _data.DisplayItems;
        PearlsCount = _data.PearlsCount;
        BlazeRodsCount = _data.BlazeRodsCount;
        BedsCount = _data.BedsCount;
        ObsidianCount = _data.ObsidianCount;
        EnderEyeCount = _data.EnderEyeCount;
    }
}
