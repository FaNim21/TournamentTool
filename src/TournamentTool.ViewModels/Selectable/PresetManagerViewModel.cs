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
using TournamentTool.Services.Managers.Lua;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Commands.Main;
using TournamentTool.ViewModels.Selectable.Preset;

namespace TournamentTool.ViewModels.Selectable;

public class PresetManagerViewModel : SelectableViewModel, IPresetNameValidator
{
    public ObservableCollection<TournamentPresetViewModel> Presets { get; set; } = [];

    private readonly ITournamentState _tournamentState;
    private readonly IDialogService _dialogService;
    private readonly ILuaScriptsManager _luaScriptsManager;
    private readonly IUIInteractionService _uiInteractionService;
    public ILoggingService Logger { get; }
    public IPresetSaver PresetService { get; }

    private TournamentPresetViewModel? _currentChosen;
    public TournamentPresetViewModel? CurrentChosen
    {
        get => _currentChosen;
        set
        {
            if (_currentChosen != null && value != null && _currentChosen.Name.Equals(value.Name)) return;
            
            _currentChosen = value;
            
            bool isEmpty = _currentChosen == null;
            if (!isEmpty)
            {
                if (_settings != null)
                {
                    _settings.LastOpenedPresetName = _currentChosen!.Name;
                }
            }

            LoadCurrentPreset(isEmpty ? string.Empty : _currentChosen!.Name);
            OnPropertyChanged(nameof(CurrentChosen));
        }
    }

    public TournamentViewModel Tournament { get; }

    public ICommand OpenControllerCommand { get; set; }
    public ICommand OpenLeaderboardCommand { get; set; }

    public ICommand ChangePresetOrderCommand { get; set; }

    public ICommand AddNewPresetCommand { get; set; }
    public ICommand SavePresetCommand { get; set; }
    public ICommand OpenPresetFolderCommand { get; set; }
    public ICommand OnItemListClickCommand { get; set; }

    public ICommand ClearCurrentPresetCommand { get; set; }
    public ICommand DuplicateCurrentPresetCommand { get; set; }
    public ICommand RenameItemCommand { get; set; }
    public ICommand RemoveCurrentPresetCommand { get; set; }

    private readonly Domain.Entities.Settings _settings;
    private readonly AppCache _appCache;

    private readonly FileSystemWatcher _fileWatcher;
    
    
    public PresetManagerViewModel(ICoordinator coordinator, IPresetSaver presetService, ITournamentState tournamentState, ITournamentPlayerRepository playerRepository,
        IBackgroundCoordinator backgroundCoordinator, ILoggingService logger, ISettingsProvider settingsProviderService, ILuaScriptsManager luaScriptsManager, 
        IDispatcherService dispatcher, INavigationService navigationService, IDialogService dialogService, IUIInteractionService uiInteractionService) : base(coordinator, dispatcher)
    {
        PresetService = presetService;
        Logger = logger;
        _tournamentState = tournamentState;
        _dialogService = dialogService; 
        _luaScriptsManager = luaScriptsManager;
        _uiInteractionService = uiInteractionService;

        Tournament = new TournamentViewModel(playerRepository, tournamentState, backgroundCoordinator, dispatcher);
        
        _settings = settingsProviderService.Get<Domain.Entities.Settings>();
        _appCache = settingsProviderService.Get<AppCache>();
        
        _fileWatcher = new FileSystemWatcher
        {
            Path = Path.Combine(Consts.PresetsPath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            Filter = "*.json",
            InternalBufferSize = 16384
        };
        _fileWatcher.Created += OnPresetAddedInFile;
        _fileWatcher.Deleted += OnPresetRemovedInFile;
        _fileWatcher.EnableRaisingEvents = true;
        
        LoadPresetsList();
        
        SavePresetCommand = new RelayCommand(presetService.SavePreset);

        OpenControllerCommand = new RelayCommand(navigationService.NavigateTo<ControllerViewModel>);
        OpenLeaderboardCommand = new RelayCommand(navigationService.NavigateTo<LeaderboardPanelViewModel>);

        ChangePresetOrderCommand = new PresetCollectionOrderChangeCommand(this);

        AddNewPresetCommand = new AddNewPresetCommand(this);
        OpenPresetFolderCommand = new RelayCommand(OpenPresetFolder);
        OnItemListClickCommand = new OnItemListClickCommand(presetService);

        ClearCurrentPresetCommand = new RelayCommand(Clear);
        DuplicateCurrentPresetCommand = new DuplicatePresetCommand(this, logger);
        RenameItemCommand = new RelayCommand(EditPresetName);
        RemoveCurrentPresetCommand = new RemovePresetCommand(this, dialogService);


        LoadStartupPreset();
    }
    public override void Dispose()
    {
        Tournament.Dispose();
        
        _fileWatcher.Created -= OnPresetAddedInFile;
        _fileWatcher.Deleted -= OnPresetRemovedInFile;
        _fileWatcher.Dispose();
    }

    public override bool CanEnable() => true;
    public override void OnEnable(object? parameter) { }
    public override bool OnDisable()
    {
        _appCache.PresetsOrder.Clear();

        for (var i = 0; i < Presets.Count; i++)
        {
            var preset = Presets[i];
            
            if (!_appCache.PresetsOrder.TryGetValue(preset.Name, out var data))
            {
                _appCache.PresetsOrder.Add(preset.Name, new PresetOrderData() {index = i});
                continue;
            }

            data.index = i;
        }
        
        return true;
    }

    private void LoadStartupPreset()
    {
        if (!_tournamentState.IsEmpty())
        {
            if (_tournamentState.CurrentPreset == null) return;
            
            TournamentPresetViewModel? currentlyOpened = Presets.FirstOrDefault(p => p.Name.Equals(_tournamentState.CurrentPreset.Name));
            _currentChosen = currentlyOpened;
            OnPropertyChanged(nameof(CurrentChosen));
            
            Tournament.IsCurrentlyOpened = true;
            
            return;
        }
        
        string lastOpened = _settings.LastOpenedPresetName;
        if (string.IsNullOrWhiteSpace(lastOpened)) return;

        TournamentPresetViewModel? lastOpenedPreset = Presets.FirstOrDefault(p => p.Name.Equals(lastOpened));
        if (lastOpenedPreset == null)
        {
            //TODO: 2 Tutaj jedna z sytuacji gdzie trzeba uwzglednic odpalany pusty preset jako wprowadznie do aplikacji
            return;
        }

        CurrentChosen = lastOpenedPreset;
    }
    private void LoadCurrentPreset(string opened)
    {
        if (string.IsNullOrEmpty(opened))
        {
            _tournamentState.ChangePreset(null);
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

            _tournamentState.ChangePreset(data);
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

        List<TournamentPresetViewModel> presetList = [];
        for (int i = presets.Length - 1; i >= 0; i--)
        {
            string text = File.ReadAllText(presets[i]) ?? string.Empty;
            try
            {
                if (string.IsNullOrEmpty(text)) continue;

                TournamentPreset? data = JsonSerializer.Deserialize<TournamentPreset>(text);
                if (data == null) continue;

                TournamentPresetViewModel presetViewModel = new(data, this, _tournamentState, Dispatcher, PresetService);
                presetList.Add(presetViewModel);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        IOrderedEnumerable<TournamentPresetViewModel> sorted = presetList.OrderBy(preset => 
            _appCache.PresetsOrder.GetValueOrDefault(preset.Name, new PresetOrderData()).index);
        
        foreach (var preset in sorted)
            Presets.Add(preset);
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
    
    public void AddDuplicatedItem(IPreset preset)
    {
        PresetService.SavePreset(preset);
        
        TournamentPreset item = new(preset.Name);
        TournamentPresetViewModel itemViewModel = new(item, this, _tournamentState, Dispatcher, PresetService);
        Presets.Add(itemViewModel);
    }
    public TournamentPresetViewModel AddNewItem(TournamentPreset preset)
    {
        TournamentPresetViewModel itemViewModel = new(preset, this, _tournamentState, Dispatcher, PresetService);
        PresetService.SavePreset(itemViewModel);
        
        Presets.Add(itemViewModel);
        return itemViewModel;
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
        
        if (_tournamentState.CurrentPreset.Name.Equals(item.Name))
        {
            _tournamentState.DeletePreset();
        }
        
        File.Delete(item.GetPath());
    }
    
    public void Clear()
    {
        var result = _dialogService.Show($"Are you sure you want to clear all data in preset: {_tournamentState.CurrentPreset.Name}", "Clearing", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        Tournament.Clear();
        _tournamentState.MarkAsModified();
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

    private void OnPresetAddedInFile(object sender, FileSystemEventArgs e)
    {
        string fullPath = e.FullPath;
        string text = File.ReadAllText(fullPath) ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;
        
        try
        {
            TournamentPreset? data = JsonSerializer.Deserialize<TournamentPreset>(text);
            if (data == null) return;
            
            TournamentPresetViewModel presetViewModel = new(data, this, _tournamentState, Dispatcher, PresetService);

            string uniqueName = Helper.GetUniqueName(presetViewModel.Name, presetViewModel.Name, IsPresetNameUnique);
            presetViewModel.Name = uniqueName;
            
            PresetService.SavePreset(presetViewModel);

            Dispatcher.Invoke(()=>
            {
                Presets.Add(presetViewModel);
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
    private void OnPresetRemovedInFile(object sender, FileSystemEventArgs e)
    {
        string name = Path.GetFileNameWithoutExtension(e.Name ?? string.Empty);
        if (string.IsNullOrEmpty(name)) return;

        Dispatcher.Invoke(()=>
        {
            RemoveItem(name);
        });
    }
    
    private void EditPresetName()
    {
        PresetService.SavePreset();
        _uiInteractionService.EnterEditModeOnHoverTextBlock();
    }
}
