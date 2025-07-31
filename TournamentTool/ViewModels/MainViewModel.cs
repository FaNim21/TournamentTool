using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Components;
using TournamentTool.Services;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Ranking;
using TournamentTool.ViewModels.Selectable;
using TournamentTool.ViewModels.StatusBar;
using TournamentTool.Windows;

namespace TournamentTool.ViewModels;

public class MainViewModel : BaseViewModel
{
    public DebugWindow? DebugWindow { get; set; }
    public TournamentViewModel TournamentViewModel { get; private set; }

    private INavigationService? _navigationService;
    public INavigationService NavigationService
    {
        get => _navigationService!;
        set
        {
            _navigationService = value;
            OnPropertyChanged(nameof(NavigationService));
        }
    }

    private StatusBarViewModel? _statusBar;
    public StatusBarViewModel? StatusBar
    {
        get => _statusBar;
        set
        {
            _statusBar = value;
            OnPropertyChanged(nameof(StatusBar));
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

    private bool _newUpdate; 
    public bool NewUpdate
    {
        get => _newUpdate;
        set
        {
            _newUpdate = value;
            OnPropertyChanged(nameof(NewUpdate));
        }
    }

    private bool _isWindowBlocked;
    public bool IsWindowBlocked
    {
        get=> _isWindowBlocked;
        set
        {
            _isWindowBlocked = value;
            OnPropertyChanged(nameof(IsWindowBlocked));
        }
    }

    public bool IsDebugWindowOpened { get; set; }
    public string VersionText { get; }
    
    public ICommand OnHamburgerClick { get; set; }
    public ICommand SelectViewModelCommand { get; set; }


    public MainViewModel(INavigationService navigationService, TournamentViewModel tournamentViewModel, StatusBarViewModel statusBar)
    {
        NavigationService = navigationService;
        TournamentViewModel = tournamentViewModel;
        StatusBar = statusBar;

        Directory.CreateDirectory(Consts.PresetsPath);
        Directory.CreateDirectory(Consts.LogsPath);
        Directory.CreateDirectory(Consts.ScriptsPath);
        Directory.CreateDirectory(Consts.LeaderboardScriptsPath);

        OnHamburgerClick = new RelayCommand(() => { IsHamburgerMenuOpen = !IsHamburgerMenuOpen; });
        SelectViewModelCommand = new RelayCommand<string>(SelectViewModel);

        VersionText = Consts.Version;
        OnPropertyChanged(nameof(VersionText));

        Task.Factory.StartNew(async () => { await CheckForUpdate(); });
    }

    public void SelectViewModel(string viewModelName)
    {
        if (NavigationService == null) return;
        
        switch (viewModelName)
        {
            case "Presets": NavigationService.NavigateTo<PresetManagerViewModel>(); break;
            case "Whitelist": NavigationService.NavigateTo<PlayerManagerViewModel>(); break;
            case "Controller": NavigationService.NavigateTo<ControllerViewModel>(); break;
            case "Leaderboard": NavigationService.NavigateTo<LeaderboardPanelViewModel>(); break;
            case "SceneManagement": NavigationService.NavigateTo<SceneManagementViewModel>(); break;
            case "Updates": NavigationService.NavigateTo<UpdatesViewModel>(); break;
            case "Settings": NavigationService.NavigateTo<SettingsViewModel>(); break;
        }
        IsHamburgerMenuOpen = false;
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
            ModifierKeys = ModifierKeys.Shift,
            Description = "Toggle Studio Mode in controller panel",
            Action = () =>
            {
                if (NavigationService.SelectedView is not ControllerViewModel controller) return;
                controller.SceneController.SwitchStudioModeCommand.Execute(null);
            }
        };

        var toggleDebugWindow = new Hotkey
        {
            Key = Key.F12,
            ModifierKeys = ModifierKeys.None,
            Description = "Toggle mode for debug window for specific selected view model",
            Action = SwitchDebugWindow
        };

        InputController.Instance.AddHotkey(renameTextBox);
        InputController.Instance.AddHotkey(toggleHamburgerMenu);
        InputController.Instance.AddHotkey(toggleStudioMode);
        InputController.Instance.AddHotkey(toggleDebugWindow);
    }

    public void BlockWindow()
    {
        IsWindowBlocked = true;
    }
    public void UnBlockWindow()
    {
        IsWindowBlocked = false;
    }

    public void UpdateDebugWindowViewModel(SelectableViewModel viewModel)
    {
        if (DebugWindow == null) return;
        ((DebugWindowViewModel)DebugWindow.DataContext).SelectedViewModel = viewModel;
    }
    private void SwitchDebugWindow()
    {
        if (!IsDebugWindowOpened)
        {
            DebugWindowViewModel viewModel = new(this)
            {
                SelectedViewModel = NavigationService.SelectedView,
            };
            DebugWindow = new DebugWindow()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow,
            };

            DebugWindow.Show();
            Application.Current.MainWindow?.Focus();
            IsDebugWindowOpened = true;
        }
        else
        {
            DebugWindow?.Close();
            DebugWindow = null;
        }
    }
}