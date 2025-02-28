using System.Collections.ObjectModel;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class Leaderboard : BaseViewModel
{
    private ObservableCollection<LeaderboardEntry> _entries = [];
    public ObservableCollection<LeaderboardEntry> Entries
    {
        get => _entries;
        set
        {
            _entries = value;
            OnPropertyChanged(nameof(Entries));
        }
    }
}