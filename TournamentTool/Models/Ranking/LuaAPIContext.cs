using TournamentTool.Factories;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models.Ranking;

//TODO: 0 Trzeba zrobic api context dla lua, zeby mozna bylo korzystac ze wszystkich dostepnych funkcji tournament toola w skrypcie
    
//TODO: 0 Trzeba zrobic klase, ktora bedzie ulatwiala trigerowanie skryptow lua i tez trzeba uwzglednic to ze za kazda zmiana presetu i zmiana skryptu w sub rule
// i ogolnie uruchomienie aplikacji trzeba cache'owac sktypty
// Najlepiej w sumie taki LuaScriptsManager, do ktorego to wszystko bedzie zbierane na potrzeby nie powielania skryptow w samych sub rules
    
//TODO: 0 Ustalic zmienne w konfiguracji sub rule, ktore beda definiowac zaleznosci przekazywane dla skryptu do tego zeby miec jak najwiecej zaleznosci pod
// bardziej zaawansowane mozliwosci ustalania punktow, typu ustalic 
    
public class LuaAPIContext
{
    private readonly Action<LeaderboardEntry> _onEntryRunRegistered;
    
    private readonly LeaderboardPlayerEvaluateData _data;
    private readonly LeaderboardSubRule _subRule;
    private readonly LeaderboardEntry _entry;

    private readonly TournamentViewModel _tournament;  // o to to trzeba bedzie pod punkty itp itd
    
    public int PlayerPosition => _entry.Position;
    public int PlayerPoints => _entry.Points;
    public int PlayerMilestoneBestTimeInMiliseconds => _entry.GetBestMilestoneTime(_data.MainSplit.Milestone);

    public int BasePoints => _subRule.BasePoints;

    public int Round => _tournament.ManagementData is RankedManagementData ranked ? ranked.Rounds : 1;


    public LuaAPIContext(LeaderboardEntry entry, LeaderboardPlayerEvaluateData data, LeaderboardSubRule subRule, TournamentViewModel tournament, Action<LeaderboardEntry> onEntryRunRegistered)
    {
        _entry = entry;
        _data = data;
        _subRule = subRule;
        _tournament = tournament;
        
        _onEntryRunRegistered = onEntryRunRegistered;
    }

    public void RegisterMilestone(int points)
    {
        var milestone = LeaderboardEntryMilestoneFactory.Create(_data, points);
        var success = _entry.AddMilestone(milestone);
        if (!success) return;

        _onEntryRunRegistered.Invoke(_entry);
    }
}
    
// public int GetBestTimeInMilestone() => 0; //TODO: 0 pobierac najlepszy czas na dany milestone
// public int GetBestAverageTimeInMilestone() => 0; //TODO: 0 pobierac najlepszy average czas na dany milestone
