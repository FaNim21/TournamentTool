using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.Main;
using TournamentTool.Components.Controls;
using TournamentTool.Enums;
using TournamentTool.Factories;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Models;
using TournamentTool.Modules.Logging;
using TournamentTool.Modules.OBS;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Selectable;

public class PresetManagerViewModel : SelectableViewModel
{
    public ObservableCollection<TournamentPreset> Presets { get; set; } = [];

    private readonly ISettings _settingsService;
    private readonly ILuaScriptsManager _luaScriptsManager;
    private IBackgroundCoordinator BackgroundCoordinator { get; }
    public ILoggingService Logger { get; }
    public TournamentViewModel TournamentViewModel { get; }
    public IPresetSaver PresetService { get; }

    private TournamentPreset? _currentChosen;
    public TournamentPreset? CurrentChosen
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

            TournamentViewModel.IsCurrentlyOpened = !isEmpty;
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


    public PresetManagerViewModel(ICoordinator coordinator, TournamentViewModel tournamentViewModel, IPresetSaver presetService, IBackgroundCoordinator backgroundCoordinator, ILoggingService logger, ISettings settingsService, ILuaScriptsManager luaScriptsManager) : base(coordinator)
    {
        TournamentViewModel = tournamentViewModel;
        PresetService = presetService;
        BackgroundCoordinator = backgroundCoordinator;
        Logger = logger;
        _settingsService = settingsService;
        _luaScriptsManager = luaScriptsManager;

        TournamentViewModel.OnControllerModeChanged += UpdateBackgroundService;
        
        LoadPresetsList();

        OpenControllerCommand = new RelayCommand(() => Coordinator.SelectViewModel("Controller"));
        OpenLeaderboardCommand = new RelayCommand(() => Coordinator.SelectViewModel("Leaderboard"));

        AddNewPresetCommand = new AddNewPresetCommand(this);
        SavePresetCommand = new RelayCommand(() => PresetService.SavePreset());
        OpenPresetFolderCommand = new RelayCommand(OpenPresetFolder);
        OnItemListClickCommand = new OnItemListClickCommand(PresetService);

        ClearCurrentPresetCommand = new ClearPresetCommand(TournamentViewModel);
        DuplicateCurrentPresetCommand = new DuplicatePresetCommand(this, PresetService);
        RenameItemCommand = new RenamePresetCommand(PresetService);
        RemoveCurrentPresetCommand = new RemovePresetCommand(this);

        LoadStartupPreset();
    }
    public override void Dispose()
    {
        TournamentViewModel.OnControllerModeChanged -= UpdateBackgroundService;
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
        if (string.IsNullOrEmpty(opened)) return;

        string filePath = Path.Combine(Consts.PresetsPath, opened + ".json");
        if (!File.Exists(filePath)) return;
            
        string text = File.ReadAllText(filePath) ?? string.Empty;
        try
        {
            if (string.IsNullOrEmpty(text)) return;
            Tournament? data = JsonSerializer.Deserialize<Tournament>(text);
            if (data == null) return;

            TournamentViewModel.ChangeData(data);
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

                data.Setup(TournamentViewModel, PresetService);
                Presets.Add(data);
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

    public void AddItem(TournamentPreset item, bool save = true)
    {
        if (save) PresetService.SavePreset(item);
        item.Setup(TournamentViewModel, PresetService);
        Presets.Add(item);
    }
    public void RemoveItem(TournamentPreset item)
    {
        Presets.Remove(item);
        if (TournamentViewModel.GetData().Name.Equals(item.Name))
        {
            TournamentViewModel.Delete();
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
}
