using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.Managers.Preset;

/// <summary>
/// TODO: 0 PODZIELIC TO NA WIECEJ SERWISOW I MIEC LEPSZA KONTROLE Z ZEWNATRZ
/// - Tournament state jako stan presetu z pomniejszymi danymi
/// - Glowna ta klasa - z zawartoscia wszystkich nastepnych wypisanych do dostepu pod serwisow
/// - Player repository?
/// - Controller mode service? i tez jakies dane w Tournament State
/// - Leaderboard service pod cala funkcjonalnosc i kontrole
/// -
/// </summary>
public class TournamentPresetManager : ITournamentPresetManager
{
    public ITournamentState State { get; }
    public ITournamentPlayerRepository PlayerRepository { get; }

    public Tournament CurrentPreset => State.CurrentPreset;
    public bool IsModified => State.IsModified;

    public string Name => State.CurrentPreset.Name;
    public bool IsPresetModified => State.IsModified;

    /*
    public ControllerMode ControllerMode
    {
        get => _tournament.ControllerMode;
        set => _tournament.ControllerMode = value;
    }
    public event Action<ControllerMode, bool>? ControllerModeChanged;
    */
    

    public TournamentPresetManager(ITournamentState state, ITournamentPlayerRepository playerRepository)
    {
        State = state;
        PlayerRepository = playerRepository;
    }

    public void MarkAsModified() => State.MarkAsModified();
    public void MarkAsUnmodified() => State.MarkAsUnmodified();
}