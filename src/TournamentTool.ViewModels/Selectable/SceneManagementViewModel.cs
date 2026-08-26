using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;
using TournamentTool.Services.Obs.Binding;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Factories;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Obs.Items;
using TournamentTool.ViewModels.UI;

namespace TournamentTool.ViewModels.Selectable;

public class SceneManagementViewModel : SelectableViewModel
{
    private readonly IBindingEngine _bindingEngine;
    private readonly IObsController _obs;
    private readonly IWindowService _windowService;
    private readonly ILoggingService _logger;
    private readonly ISettingsSaver _settingsSaver;

    public SceneEditorViewModel SceneEditor { get; }

    public ReadOnlyObservableCollection<SceneDto> Scenes { get; }
    public ObservableCollection<TreeItemViewModel<SceneItemViewModel>> TreeItems { get; } = [];

    private SceneDto? PreviousSelectecScene;
    public SceneDto? SelectedScene
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    public TreeItemViewModel<SceneItemViewModel>? SelectedSceneItem
    {
        get;
        set
        {
            SceneItemContentAction(field, item => item.UnFocus());

            field = value;
            OnPropertyChanged();

            SceneItemContentAction(field, item => item.Focus());
        }
    }

    public Dimension SceneDimension
    {
        get;
        set
        {
            field = value;
            SceneEditor.ResizeScene(value);
        }
    } = new(-1, -1);
    public int SceneRefreshTrigger
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 0;
    public int SceneItemsRefreshTrigger
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 0;

    public ICommand EditSceneItemCommand { get; }

    private ObsConfiguration _obsConfig;


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
    ///
    /// INNE POMYSLY (Automatt):
    /// 1. tworzenie grupy pova za pomocą 1 przycisku w TT (presety defaultowe oraz własne, wybierasz który chcesz i bang wszystko się samo tworzy, rozmiary pozycje itp itd, grupuje się w obsie i miód malina)
    /// 2. wybierasz parę źródeł na Scene Configu, klikasz prawym i "add as a preset" i tworzy to nowy preset do rzeczy opisanych na górze
    /// </summary>
    public SceneManagementViewModel(IDispatcherService dispatcher, IBindingEngine bindingEngine, ISettingsProvider settingsProvider,
        ISceneControllerViewModelFactory sceneControllerFactory, IObsController obs, IWindowService windowService, ILoggingService logger,
        ISettingsSaver settingsSaver) : base(dispatcher)
    {
        _bindingEngine = bindingEngine;
        _obs = obs;
        _windowService = windowService;
        _logger = logger;
        _settingsSaver = settingsSaver;

        _obsConfig = settingsProvider.Get<ObsConfiguration>();

        SceneEditor = sceneControllerFactory.CreateEditor();
        Scenes = SceneEditor.Scenes;

        EditSceneItemCommand = new RelayCommand<SceneItemViewModel>(EditSceneItem);
        SceneEditor.SelectedSceneChangedCommand = new AsyncRelayCommand(OnSelectedSceneChanged);
    }
    public override void OnEnable(object? parameter)
    {
        SceneEditor.OnEnable(null);

        if (string.IsNullOrEmpty(SceneEditor.MainSceneViewModel.SceneUuid)) return;
        
        SelectedScene = Scenes.FirstOrDefault(s => s.Uuid.Equals(SceneEditor.MainSceneViewModel.SceneUuid));
    }
    public override bool OnDisable()
    {
        SceneEditor.OnDisable();
        _settingsSaver.Save();
        
        return true;
    }

    private void EditSceneItem(SceneItemViewModel sceneItemViewModel)
    {
        SceneItemEditWindowViewModel viewModel = new(sceneItemViewModel, SceneEditor.MainSceneViewModel, _bindingEngine, _obsConfig, Dispatcher);
        _windowService.ShowCustomDialog(viewModel, OnEditSceneItemClosed, "SceneItemEditWindow");
    }

    private async void OnEditSceneItemClosed(SceneItemEditWindowViewModel editWindowViewModel)
    {
        try
        {
            BindingKey key = editWindowViewModel.GetBindingKey();
            SceneItemConfiguration editedConfig = new(editWindowViewModel.InputKind, key);
            string uuid = editWindowViewModel.SceneItemViewModel.SourceUUID;
            
            _obsConfig.SceneItemConfigs[uuid] = editedConfig;
            await SceneEditor.UpdateScenes(uuid);
            _bindingEngine.PublishAll();
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }
    
    private async Task OnSelectedSceneChanged(CancellationToken token)
    {
        if (SelectedScene == null || PreviousSelectecScene == SelectedScene) return;
        
        PreviousSelectecScene = SelectedScene;
        
        TreeItems.Clear();
        await SceneEditor.MainSceneViewModel.NewSceneAsync(SelectedScene.Name, SelectedScene.Uuid);

        foreach (var sceneItem in SceneEditor.MainSceneViewModel.SceneItems)
        {
            if (sceneItem is not GroupItemViewModel) continue;
            
            TreeItems.Add(new TreeItemViewModel<SceneItemViewModel>(Dispatcher, sceneItem)
            {
                Header = sceneItem.SourceName
            });
        }

        SceneEditor.MainSceneViewModel.Refresh();
        
        foreach (var sceneItem in SceneEditor.MainSceneViewModel.SceneItems)
        {
            bool found = false;
            
            foreach (var treeItem in TreeItems)
            {
                if (treeItem.Content.SourceUUID.Equals(sceneItem.SourceUUID))
                {
                    found = true;
                }
                if (!treeItem.Header.Equals(sceneItem.GroupName)) continue;
                
                treeItem.SubItems.Add(new TreeItemViewModel<SceneItemViewModel>(Dispatcher, sceneItem)
                {
                    Header = sceneItem.SourceName
                });
                    
                found = true;
                break;
            }
            
            if (found) continue;

            TreeItems.Add(new TreeItemViewModel<SceneItemViewModel>(Dispatcher, sceneItem)
            {
                Header = sceneItem.SourceName
            });
        }
    }
    
    private static void SceneItemContentAction(TreeItemViewModel<SceneItemViewModel>? treeItem, Action<SceneItemViewModel> contentAction)
    {
        if (treeItem is null) return;
        
        if (treeItem.Content is GroupItemViewModel)
        {
            foreach (var subItem in treeItem.SubItems)
            {
                contentAction(subItem.Content);
            }
        }
        else
        {
            contentAction(treeItem.Content);
        }
    }
}