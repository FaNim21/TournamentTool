using System.Collections.ObjectModel;
using System.Windows.Input;
using ObsWebSocket.Core.Protocol.Common;
using ObsWebSocket.Core.Protocol.Responses;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Obs;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Factories;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels.Selectable;

public class SceneManagementViewModel : SelectableViewModel
{
    private readonly IObsCommunicationProvider _obsCommunicationProvider;
    private readonly ObsController _obs;

    public SceneControllerViewmodel SceneController { get; }

    public ObservableCollection<SceneStub> Scenes { get; set; } = [];

    private SceneStub? _selectedScene;
    public SceneStub? SelectedScene
    {
        get => _selectedScene;
        set
        {
            _selectedScene = value;
            OnPropertyChanged(nameof(SelectedScene));
        }
    }
    
    private SceneItemViewModel? _selectedSceneItem;
    public SceneItemViewModel? SelectedSceneItem
    {
        get => _selectedSceneItem;
        set
        {
            _selectedSceneItem = value;
            OnPropertyChanged(nameof(SelectedSceneItem));
        }
    }

    private Dimension _sceneDimension = new(-1, -1);
    public Dimension SceneDimension
    {
        get => _sceneDimension;
        set
        {
            _sceneDimension = value;
            SceneController.ResizeScene(value);
        }
    }

    private int _sceneRefreshTrigger = 0;
    public int SceneRefreshTrigger
    {
        get => _sceneRefreshTrigger;
        set
        {
            _sceneRefreshTrigger = value;
            OnPropertyChanged(nameof(SceneRefreshTrigger));
        }
    }
    
    private int _sceneItemsRefreshTrigger = 0;
    public int SceneItemsRefreshTrigger
    {
        get => _sceneItemsRefreshTrigger;
        set
        {
            _sceneItemsRefreshTrigger = value;
            OnPropertyChanged(nameof(SceneItemsRefreshTrigger));
        }
    }
    
    public ICommand EditSceneItemCommand { get; private set; }

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
    public SceneManagementViewModel(IDispatcherService dispatcher, IObsCommunicationProvider obsCommunicationProvider, 
        ISceneControllerViewModelFactory sceneControllerFactory, ObsController obs) : base(dispatcher)
    {
        _obsCommunicationProvider = obsCommunicationProvider;
        _obs = obs;

        SceneController = sceneControllerFactory.Create();
        SceneController.IsStudioModeSupported = false;

        EditSceneItemCommand = new RelayCommand<SceneItemViewModel>(EditSceneItem);
    }
    public override void OnEnable(object? parameter)
    {
        SceneController.OnEnable(null);

        Task.Run(async () =>
        {
            GetSceneListResponseData? sceneResponse = await _obs.GetSceneList();
            if (sceneResponse == null) return;

            await Dispatcher.InvokeAsync(() =>
            {
                Scenes = new ObservableCollection<SceneStub>(sceneResponse.Scenes ?? []);
                OnPropertyChanged(nameof(Scenes));
            });
        });
        
    }
    public override bool OnDisable()
    {
        SceneController.OnDisable();
        
        return true;
    }

    private void EditSceneItem(SceneItemViewModel sceneItem)
    {
        //TODO: 0 Odpalac okno edycji scene item
    }
}