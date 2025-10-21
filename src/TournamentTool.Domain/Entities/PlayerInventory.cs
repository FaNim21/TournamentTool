namespace TournamentTool.Domain.Entities;

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

