using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Preset;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Lua;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Commands.Main;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable.Preset;

namespace TournamentTool.ViewModels.Selectable;

public class PresetManagerViewModel : SelectableViewModel
{
    public ObservableCollection<TournamentPresetViewModel> Presets { get; set; } = [];

    private readonly ISettings _settingsService;
    private readonly ILuaScriptsManager _luaScriptsManager;
    private readonly IUIInteractionService _uiInteractionService;
    private IBackgroundCoordinator BackgroundCoordinator { get; }
    public ILoggingService Logger { get; }
    public ITournamentPresetManager Tournament { get; }
    public IPresetSaver PresetService { get; }

    private TournamentPresetViewModel? _currentChosen;
    public TournamentPresetViewModel? CurrentChosen
    {
        get => _currentChosen;
        set
        {
            _currentChosen = value;

            bool isEmpty = _currentChosen == null;
            if (!isEmpty)
            {
                SaveLastOpened(_currentChosen!.Name);
            }

            LoadCurrentPreset(isEmpty ? string.Empty : _currentChosen!.Name);
            OnPropertyChanged(nameof(CurrentChosen));
        }
    }

    public ICommand OpenControllerCommand { get; set; }
    public ICommand OpenLeaderboardCommand { get; set; }

    public ICommand AddNewPresetCommand { get; set; }
    public ICommand SavePresetCommand { get; set; }
    public ICommand OpenPresetFolderCommand { get; set; }
    public ICommand OnItemListClickCommand { get; set; }

    public ICommand ClearCurrentPresetCommand { get; set; }
    public ICommand DuplicateCurrentPresetCommand { get; set; }
    public ICommand RenameItemCommand { get; set; }
    public ICommand RemoveCurrentPresetCommand { get; set; }


    public PresetManagerViewModel(ICoordinator coordinator, ITournamentPresetManager tournament, IPresetSaver presetService, 
        IBackgroundCoordinator backgroundCoordinator, ILoggingService logger, ISettings settingsService, ILuaScriptsManager luaScriptsManager, 
        IDispatcherService dispatcher, INavigationService navigationService, IDialogService dialogService, IUIInteractionService uiInteractionService) : base(coordinator, dispatcher)
    {
        Tournament = tournament;
        PresetService = presetService;
        BackgroundCoordinator = backgroundCoordinator;
        Logger = logger;
        _settingsService = settingsService;
        _luaScriptsManager = luaScriptsManager;
        _uiInteractionService = uiInteractionService;

        Tournament.OnControllerModeChanged += UpdateBackgroundService;
        
        LoadPresetsList();

        OpenControllerCommand = new RelayCommand(navigationService.NavigateTo<ControllerViewModel>);
        OpenLeaderboardCommand = new RelayCommand(navigationService.NavigateTo<LeaderboardPanelViewModel>);

        AddNewPresetCommand = new AddNewPresetCommand(this);
        SavePresetCommand = new RelayCommand(() => PresetService.SavePreset());
        OpenPresetFolderCommand = new RelayCommand(OpenPresetFolder);
        OnItemListClickCommand = new OnItemListClickCommand(PresetService);

        ClearCurrentPresetCommand = new RelayCommand(tournament.Clear);
        DuplicateCurrentPresetCommand = new DuplicatePresetCommand(this, PresetService);
        RenameItemCommand = new RelayCommand(EditPresetName);
        RemoveCurrentPresetCommand = new RemovePresetCommand(this, dialogService);

        LoadStartupPreset();
    }
    public override void Dispose()
    {
        Tournament.OnControllerModeChanged -= UpdateBackgroundService;
    }

    public override bool CanEnable() { return true; }
    public override void OnEnable(object? parameter) { }
    public override bool OnDisable()
    {
        return true;
    }

    private void LoadStartupPreset()
    {
        string lastOpened = _settingsService.Settings.LastOpenedPresetName;
        for (int i = 0; i < Presets.Count; i++)
        {
            if (!Presets[i].Name.Equals(lastOpened)) continue;
            
            CurrentChosen = Presets[i];
            break;
        }
    }
    private void LoadCurrentPreset(string opened)
    {
        if (string.IsNullOrEmpty(opened))
        {
            Tournament.ChangeData(null);
            return;
        }

        string filePath = Path.Combine(Consts.PresetsPath, opened + ".json");
        if (!File.Exists(filePath)) return;
            
        string text = File.ReadAllText(filePath) ?? string.Empty;
        try
        {
            if (string.IsNullOrEmpty(text)) return;
            Tournament? data = JsonSerializer.Deserialize<Tournament>(text);
            if (data == null) return;

            Tournament.ChangeData(data);
            _luaScriptsManager.LoadLuaScripts();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
    private void LoadPresetsList()
    {
        var presets = Directory.GetFiles(Consts.PresetsPath, "*.json", SearchOption.TopDirectoryOnly).AsSpan();
        for (int i = presets.Length - 1; i >= 0; i--)
        {
            string text = File.ReadAllText(presets[i]) ?? string.Empty;
            try
            {
                if (string.IsNullOrEmpty(text)) continue;

                TournamentPreset? data = JsonSerializer.Deserialize<TournamentPreset>(text);
                if (data == null) continue;

                TournamentPresetViewModel presetViewModel = new(data, Tournament, Dispatcher);
                Presets.Add(presetViewModel);
            }
            catch { /**/ }
        }
    }

    public bool IsPresetNameUnique(string name)
    {
        for (int i = 0; i < Presets.Count; i++)
        {
            var current = Presets[i];
            if (current.Name!.Equals(name, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    public void AddItem(IPreset preset)
    {
        PresetService.SavePreset(preset);
        AddItem(new TournamentPreset(preset.Name));
    }
    public void AddItem(TournamentPreset item)
    {
        TournamentPresetViewModel itemViewModel = new(item, Tournament, Dispatcher);
        Presets.Add(itemViewModel);
    }

    public void RemoveItem(string name)
    {
        foreach (var preset in Presets)
        {
            if (!preset.Name.Equals(name)) continue;
            
            RemoveItem(preset);
            break;
        }
    }
    public void RemoveItem(TournamentPresetViewModel item)
    {
        Presets.Remove(item);
        if (Tournament.GetData().Name.Equals(item.Name))
        {
            Tournament.Delete();
        }
        File.Delete(item.GetPath());
    }

    private void OpenPresetFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Consts.PresetsPath,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    public void SaveLastOpened(string presetName)
    {
        _settingsService.Settings.LastOpenedPresetName = presetName;
    }

    private void UpdateBackgroundService(ControllerMode mode, bool isValidated)
    {
        BackgroundCoordinator.Initialize(mode, isValidated);
    }

    private void EditPresetName()
    {
        PresetService.SavePreset();
        _uiInteractionService.EnterEditModeOnHoverTextBlock();
    }
}
