using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Components;
using TournamentTool.Models;
using TournamentTool.Services;
using TournamentTool.Utils;
using TournamentTool.Windows;

namespace TournamentTool.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly JsonSerializerOptions _serializerOptions;

    public DebugWindow? DebugWindow { get; set; }

    public string VersionText { get; set; }

    public List<SelectableViewModel> baseViewModels = [];

    public Tournament? Configuration { get; set; }

    private INavigationService? _navigationService;
    public INavigationService NavigationService
    {
        get => _navigationService;
        set
        {
            _navigationService = value;
            OnPropertyChanged(nameof(NavigationService));
        }
    }

    private bool _isHamburgerMenuOpen = false;
    public bool IsHamburgerMenuOpen
    {
        get => _isHamburgerMenuOpen;
        set
        {
            if (NavigationService.SelectedView is UpdatesViewModel updates && updates.Downloading) return;
            if (_isHamburgerMenuOpen == value) return;

            _isHamburgerMenuOpen = value;
            OnPropertyChanged(nameof(IsHamburgerMenuOpen));
        }
    }

    private bool _newUpdate { get; set; }
    public bool NewUpdate
    {
        get => _newUpdate;
        set
        {
            _newUpdate = value;
            OnPropertyChanged(nameof(NewUpdate));
        }
    }

    public ICommand OnHamburegerClick { get; set; }
    public ICommand SelectViewModelCommand { get; set; }


    public MainViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;

        if (!Directory.Exists(Consts.PresetsPath))
            Directory.CreateDirectory(Consts.PresetsPath);

        _serializerOptions = new JsonSerializerOptions() { WriteIndented = true };

        OnHamburegerClick = new RelayCommand(() => { IsHamburgerMenuOpen = !IsHamburgerMenuOpen; });
        SelectViewModelCommand = new RelayCommand<string>(SelectViewModel);

        VersionText = Consts.Version;
        OnPropertyChanged(nameof(VersionText));

        Task.Factory.StartNew(async () =>
        {
            await CheckForUpdate();
        });
    }

    public void SelectViewModel(string viewModelName)
    {
        switch (viewModelName)
        {
            case "Presets": NavigationService.NavigateTo<PresetManagerViewModel>(Configuration!); break;
            case "Whitelist": NavigationService.NavigateTo<PlayerManagerViewModel>(Configuration!); break;
            case "Controller": NavigationService.NavigateTo<ControllerViewModel>(Configuration!); break;
            case "Updates": NavigationService.NavigateTo<UpdatesViewModel>(Configuration!); break;
            case "Settings": NavigationService.NavigateTo<SettingsViewModel>(Configuration!); break;
        }
        IsHamburgerMenuOpen = false;
    }

    public void SavePreset(Tournament? configuration = null)
    {
        configuration ??= Configuration;
        if (configuration == null) return;

        var data = JsonSerializer.Serialize<object>(configuration, _serializerOptions);

        string path = configuration.GetPath();
        File.WriteAllText(path, data);

        if (NavigationService.SelectedView is not PresetManagerViewModel presetManager) return;
        presetManager.PresetIsSaved();
    }

    private async Task CheckForUpdate()
    {
        UpdateChecker updateChecker = new();
        bool isNewUpdate = false;

        try
        {
            isNewUpdate = await updateChecker.CheckForUpdates();
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
        }

        NewUpdate = isNewUpdate;
    }

    public void HotkeySetup()
    {
        var renameTextBox = new Hotkey
        {
            Key = Key.F2,
            ModifierKeys = ModifierKeys.None,
            Description = "Triggers renaming elements for now mainly in preset panel",
            Action = () =>
            {
                var textBlock = Helper.GetFocusedUIElement<EditableTextBlock>();
                if (textBlock is { IsEditable: true })
                    textBlock.IsInEditMode = true;
            }
        };

        var toggleHamburgerMenu = new Hotkey
        {
            Key = Key.F1,
            ModifierKeys = ModifierKeys.None,
            Description = "Toggle visibility for hamburger menu",
            Action = () =>
            {
                IsHamburgerMenuOpen = !IsHamburgerMenuOpen;
            }
        };

        var toggleStudioMode = new Hotkey
        {
            Key = Key.S,
            ModifierKeys = ModifierKeys.None,
            Description = "Toggle Sudio Mode in controller panel",
            Action = () =>
            {
                if (NavigationService.SelectedView is not ControllerViewModel controller) return;
                controller.OBS.SwitchStudioModeCommand.Execute(null);
            }
        };

        var toggleDebugWindow = new Hotkey
        {
            Key = Key.F12,
            ModifierKeys = ModifierKeys.None,
            Description = "Toggle mode for debug window for specific selected view model",
            Action = () =>
            {
                SwitchDebugWindow();
            }
        };

        InputController.Instance.AddHotkey(renameTextBox);
        InputController.Instance.AddHotkey(toggleHamburgerMenu);
        InputController.Instance.AddHotkey(toggleStudioMode);
        InputController.Instance.AddHotkey(toggleDebugWindow);
    }

    private void UpdateDebugWindowViewModel(SelectableViewModel viewModel)
    {
        if (DebugWindow == null) return;
        ((DebugWindowViewModel)DebugWindow.DataContext).SelectedViewModel = viewModel;
    }
    public void SwitchDebugWindow()
    {
        if (DebugWindow == null)
        {
            DebugWindowViewModel viewModel = new()
            {
                SelectedViewModel = NavigationService.SelectedView,
            };
            DebugWindow = new DebugWindow()
            {
                DataContext = viewModel
            };
            DebugWindow.Show();
            Application.Current.MainWindow.Focus();
        }
        else
        {
            ((DebugWindowViewModel)DebugWindow.DataContext).SelectedViewModel = null;
            DebugWindow.Close();
            DebugWindow = null;
        }
    }
}