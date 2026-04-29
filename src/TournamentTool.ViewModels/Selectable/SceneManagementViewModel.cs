using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Obs;
using TournamentTool.Services.Obs.Binding;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Factories;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Selectable;

public class SceneManagementViewModel : SelectableViewModel
{
    private readonly IBindingEngine _bindingEngine;
    private readonly IObsController _obs;
    private readonly IWindowService _windowService;

    public SceneEditorViewModel SceneEditor { get; }

    public ReadOnlyObservableCollection<SceneDto> Scenes { get; }

    private SceneDto? _selectedScene;
    public SceneDto? SelectedScene
    {
        get => _selectedScene;
        set
        {
            _selectedScene = value;
            OnPropertyChanged();
        }
    }
    
    private SceneItemViewModel? _selectedSceneItem;
    public SceneItemViewModel? SelectedSceneItem
    {
        get => _selectedSceneItem;
        set
        {
            _selectedSceneItem?.UnFocus();
            
            _selectedSceneItem = value;
            OnPropertyChanged();
            
            _selectedSceneItem?.Focus();
        }
    }

    private Dimension _sceneDimension = new(-1, -1);
    public Dimension SceneDimension
    {
        get => _sceneDimension;
        set
        {
            _sceneDimension = value;
            SceneEditor.ResizeScene(value);
        }
    }

    private int _sceneRefreshTrigger = 0;
    public int SceneRefreshTrigger
    {
        get => _sceneRefreshTrigger;
        set
        {
            _sceneRefreshTrigger = value;
            OnPropertyChanged();
        }
    }
    
    private int _sceneItemsRefreshTrigger = 0;
    public int SceneItemsRefreshTrigger
    {
        get => _sceneItemsRefreshTrigger;
        set
        {
            _sceneItemsRefreshTrigger = value;
            OnPropertyChanged();
        }
    }
    
    public ICommand EditSceneItemCommand { get; }

    private AppCache _appCache;


    /// <summary>
    /// ViewModel nie moze byc glownym miejscem technicznej komunikacji, tylko rzeczywistym posrednikiem miedzy logika a UI
    /// Takze jak bedzie wygladac design komunikacji:
    /// - Trzeba uzywac uuid do przechwytywania danych o scene itemie w app cache
    ///
    /// Jak powinno byc rozbite UI (POWINNO BYC PROSTE I NIE OBCIAZAJACE W ZAWARTOSC Dla uzytkownika, czyli bez dodawania/usuwania elementow):
    /// - Ogolnie cale UI scene managementtu powinno sie opierac o jak najwiecej potrzebnych kontrolek w celu obslugi OBS'a ze strony TT,
    /// - lista z itemami na scenie powinna byc filtrowana na typ i nazwe,
    /// - lista ze scenami,
    /// - lista z scene collection (jakos schowana poniewaz nie jest ciagle potrzebna)
    /// - panel do listy ze skryptami, czyli dodawanie/usuwanie/edycja
    ///     — w panelu jest opcja do podpięcia się pod istniejacy item?
    ///     — trzeba zdecydowac czy sie bedzie pisalo skrypt i wtedy w nim rejestrowalo custom zmienna, ktora wtedy byla by na przyklad taki drop down'em
    ///       w celu wyboru itemu? wtedy z poziomu skryptu ustala sie tym customowej zmiennej miedzy typem scene itemu (enum)
    ///     — zaprojektowac trzeba API LUA, czyli jakie eventy beda dostepne do przechwytywania, jak OnTextChanged dla textfieldo,
    ///       czy OnSidePanelUpdate do przechwycenia informacji z bocznego panelu w celu aktualizacji scene itemu dla ktorego jest zrobiony skrypt
    /// </summary>
    public SceneManagementViewModel(IDispatcherService dispatcher, IBindingEngine bindingEngine, ISettingsProvider settingsProvider,
        ISceneControllerViewModelFactory sceneControllerFactory, IObsController obs, IWindowService windowService) 
        : base(dispatcher)
    {
        _bindingEngine = bindingEngine;
        _obs = obs;
        _windowService = windowService;

        _appCache = settingsProvider.Get<AppCache>();

        SceneEditor = sceneControllerFactory.CreateEditor();
        Scenes = SceneEditor.Scenes;
        
        //TODO: 0 Przechwytywac eventy z OBS'a

        EditSceneItemCommand = new RelayCommand<SceneItemViewModel>(EditSceneItem);
        SceneEditor.SelectedSceneChangedCommand = new AsyncRelayCommand<SceneDto>(OnSelectedSceneChanged);
    }
    public override void OnEnable(object? parameter)
    {
        SceneEditor.OnEnable(null);

        if (string.IsNullOrEmpty(SceneEditor.MainSceneViewModel.SceneUuid)) return;
        
        _selectedScene = Scenes.FirstOrDefault(s => s.Uuid.Equals(SceneEditor.MainSceneViewModel.SceneUuid));
        OnPropertyChanged(nameof(SelectedScene));
    }
    public override bool OnDisable()
    {
        SceneEditor.OnDisable();
        
        return true;
    }

    private void EditSceneItem(SceneItemViewModel sceneItem)
    {
        SceneItemEditWindowViewModel viewModel = new(sceneItem, _bindingEngine, _appCache, Dispatcher);
        _windowService.ShowCustomDialog(viewModel, OnEditSceneItemClosed, "SceneItemEditWindow");
    }

    private void OnEditSceneItemClosed(SceneItemEditWindowViewModel editWindowViewModel)
    {
        SceneItemConfiguration config = new(editWindowViewModel.InputKind, editWindowViewModel.BindingKey);
        
        // _bindingEngine.UpsertItem(config.Key, config.Value.BindingKey); // Tez trzeba resetowac binding.UpsertItem z BindingSchemaInitializer
        _appCache.SceneItemConfigs[editWindowViewModel.SceneItemViewModel.SourceUUID] = config;
        
        //TODO: 0 To nie koniecznie moze miec sens, poniewaz trzeba ogarnac to zeby nie bylo duplikatow
        // i tez tylko z glownego scene managera byly dane w bindingu, takze tez wtedy tutaj najlepiej bylo by od razu update'owac
        // oryginalnie odpalone sceny zeby zmiany wchodzily live
        
        //TODO: 0 To pozniej, bo w sumie
        //SceneEditor.UpdateBinding(BindingKey.Empty(), string.Empty);
        
        editWindowViewModel.SceneItemViewModel.SceneItem.UpdateBinding(editWindowViewModel.BindingKey);
        SceneEditor.MainSceneViewModel.Refresh();
    }
    
    private async Task OnSelectedSceneChanged(SceneDto? selectedScene, CancellationToken token)
    {
        if (selectedScene == null) return;
        
        //TODO: 0 problem tutaj jest taki, ze trzeba uwazac na to, ze jest razem z tym modyfikowana scena w glownym scene manager
        // a tez nie wiem czy chce zeby ona byla tutaj celem do edycji, jezeli sobie dziala w tle pod bindingi itp itd
        // ale w teorii wszystkie sceny sa lapane do aktualizacji i trzeba sprawdzic, czy to trzymanie glownej sceny cos w sumie zmienia
        // bo niby mialo byc do dynamicznej aktualizacji
        await SceneEditor.MainSceneViewModel.NewSceneAsync(selectedScene.Name, selectedScene.Uuid);
    }
}