using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class PlayerInventory : BaseViewModel
{
    private bool _displayItems { get; set; } = false;
    public bool DisplayItems
    {
        get => _displayItems;
        set
        {
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
            _enderEyeCount = value;
            OnPropertyChanged(nameof(EnderEyeCount));
        }
    }


    //TODO: 0 To ewentualnie zrobic jako liste itemow zamiast kazdy item z kolei zeby ulatwic aktualizacje, ale narazie jest to totalnie zbedne do testow
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
