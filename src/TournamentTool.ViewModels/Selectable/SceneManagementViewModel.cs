using System.Collections.ObjectModel;
using System.Windows.Input;
using ObsWebSocket.Core.Protocol.Common;
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

    public SceneControllerViewModel SceneController { get; }

    public ReadOnlyObservableCollection<SceneDto> Scenes { get; }

    private SceneDto? _selectedScene;
    public SceneDto? SelectedScene
    {
        get => _selectedScene;
        set
        {
            _selectedScene = value;
            OnPropertyChanged();
            
            OnSelectedSceneChanged(value);
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
    
    public ICommand EditSceneItemCommand { get; private set; }

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

        SceneController = sceneControllerFactory.Create(isStudioModeSupported: false);
        Scenes = SceneController.Scenes;
        
        //TODO: 0 Przechwytywac eventy z OBS'a

        EditSceneItemCommand = new RelayCommand<SceneItemViewModel>(EditSceneItem);
    }
    public override void OnEnable(object? parameter)
    {
        SceneController.OnEnable(null);

        if (string.IsNullOrEmpty(SceneController.MainSceneViewModel.SceneUuid)) return;
        
        _selectedScene = Scenes.FirstOrDefault(s => s.Uuid.Equals(SceneController.MainSceneViewModel.SceneUuid));
        OnPropertyChanged(nameof(SelectedScene));
    }
    public override bool OnDisable()
    {
        SceneController.OnDisable();
        
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
        
        _appCache.SceneItemConfigs[editWindowViewModel.SceneItem.SourceUUID] = config;
        
        string sceneName = SceneController.MainSceneViewModel.SceneName;
        string sceneUuid = SceneController.MainSceneViewModel.SceneUuid;
        SceneController.MainSceneViewModel.SetSceneItems(sceneName, sceneUuid, true);
    }
    
    private void OnSelectedSceneChanged(SceneDto? selectedScene)
    {
        if (selectedScene == null) return;
        
        SceneController.MainSceneViewModel.SetSceneItems(selectedScene.Name, selectedScene.Uuid);
    }
}