using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels;

public class LeaderboardEntryViewViewModel : BaseViewModel
{
    private LeaderboardEntryViewModel _entry = new(null!, null);
    public LeaderboardEntryViewModel Entry
    {
        get => _entry;
        set
        {
            _entry = value;
            OnPropertyChanged(nameof(Entry));
        }
    }
    
    
    public LeaderboardEntryViewViewModel(LeaderboardEntryViewModel entry)
    {
        Entry = entry;
    }
}