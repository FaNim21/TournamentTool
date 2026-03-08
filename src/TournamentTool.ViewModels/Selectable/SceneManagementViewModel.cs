using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Services.Obs;

namespace TournamentTool.ViewModels.Selectable;

public class SceneManagementViewModel : SelectableViewModel
{
    private readonly IObsCommunicationProvider _obsCommunicationProvider;

    /// <summary>
    /// ViewModel nie moze byc glownym miejscem technicznej komunikacji, tylko rzeczywistym posrednikiem miedzy logika a UI
    /// Takze jak bedzie wygladac design komunikacji:
    /// - trzeba sie porozumiewac z itemami po ID, gdzie kluczem bedzie nazwa itemu z racji czytelnosci dla uzytkownika, ale trzeba od razu wyciagac z tego id
    ///   chyba ze okaze sie to jednak nie oplacalne z racji komunikacji ze skryptami to wtedy zostaje przy nazwie itemu
    /// -
    /// -
    ///
    /// Jak powinno byc rozbite UI (POWINNO BYC PROSTE I NIE OBCIAZAJACE W ZAWARTOSC):
    /// - Ogolnie cale UI scene managementtu powinno sie opierac o jak najwiecej potrzebnych kontrolek w celu obslugi OBS'a ze strony TT,
    /// - SCENA — powinna byc oddzielna kontrolka do tego zeby wyswietlac wszystkie wspierane elementy sceny
    ///     — powinno wyswietlac wszystkie itemy, gdzie text fieldy powinny miec przezroczystosc i tak samo browsery zaleznie od typu (nie wiem jeszcze jak to rozroznic)
    ///     — zaznaczenie itemu z listy w scene managerze powinno zaznaczac ten element w scenie i wylaczac mu przezroczystosc
    ///     —
    /// - lista z itemami na scenie powinna byc filtrowana na typ i nazwe,
    /// - lista ze scenami (jakos schowana poniewaz nie jest ciagle potrzebna)
    /// - lista z scene collection (jakos schowana poniewaz nie jest ciagle potrzebna)
    /// - panel do listy ze skryptami, czyli dodawanie/usuwanie/edycja
    ///     — w panelu jest opcja do podpięcia się pod istniejacy item?
    ///     — trzeba zdecydowac czy sie bedzie pisalo skrypt i wtedy w nim rejestrowalo custom zmienna, ktora wtedy byla by na przyklad taki drop down'em
    ///       w celu wyboru itemu? wtedy z poziomu skryptu ustala sie tym customowej zmiennej miedzy typem scene itemu (enum)
    ///     — zaprojektowac trzeba API LUA, czyli jakie eventy beda dostepne do przechwytywania, jak OnTextChanged dla textfieldo,
    ///       czy OnSidePanelUpdate do przechwycenia informacji z bocznego panelu w celu aktualizacji scene itemu dla ktorego jest zrobiony skrypt
    /// - 
    ///     
    /// </summary>
    public SceneManagementViewModel(IDispatcherService dispatcher, IObsCommunicationProvider obsCommunicationProvider) : base(dispatcher)
    {
        _obsCommunicationProvider = obsCommunicationProvider;
    }
}