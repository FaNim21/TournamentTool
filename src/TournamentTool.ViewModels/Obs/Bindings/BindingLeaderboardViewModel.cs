using System.Collections.ObjectModel;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Obs;

namespace TournamentTool.ViewModels.Obs.Bindings;

public class BindingLeaderboardViewModel : BindingViewModelBase
{
    private int _position = 0;
    public int Position
    {
        get => _position;
        set
        {
            _position = value;
            OnPropertyChanged();
        }
    }
    
    
    public BindingLeaderboardViewModel(ObservableCollection<string> leaderboardSchemas,
        BindingKey? bindingKey, IDispatcherService dispatcher) : base(dispatcher)
    {
        Fields = leaderboardSchemas;
        
        if (bindingKey is not BindingKeyLeaderboard leaderboardKey) return;
        if (leaderboardKey.IsEmpty()) return;
        
        ChosenField = Fields.FirstOrDefault(field => field.Equals(leaderboardKey.Field, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    public override BindingKey GetBindingKey()
    {
        return BindingKey.CreateLeaderboard(ChosenField, Position);
    }
}