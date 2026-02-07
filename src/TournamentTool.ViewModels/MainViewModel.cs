using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.External;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.Services.State;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Logging;
using TournamentTool.ViewModels.Selectable;
using TournamentTool.ViewModels.StatusBar;

namespace TournamentTool.ViewModels;

public class MainViewModel : BaseViewModel
{
    public ILogStore LogStore { get; }
    public ILoggingService Logger { get; }
    public IPresetSaver PresetSaver { get; }
    public ITournamentState TournamentState { get; }
    public INavigationService NavigationService { get; }

    private readonly IUpdateCheckerService _updateChecker;
    private readonly IApplicationState _applicationState;
    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;

    public DebugWindowViewModel? DebugWindowViewModel { get; private set; }
    
    public StatusBarViewModel? StatusBar { get; }
    public NotificationPanelViewModel NotificationPanel { get; }
    public ConsoleViewModel Console { get; }

    private bool _isHamburgerMenuOpen;
    public bool IsHamburgerMenuOpen
    {
        get => _isHamburgerMenuOpen;
        set
        {
            if (IsWindowBlocked) return;
            if (NavigationService.SelectedView is UpdatesViewModel { Downloading: true }) return;
            if (_isHamburgerMenuOpen == value) return;

            NotificationPanel?.HidePanel();
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
    
    public ICommand SelectViewModelCommand { get; private set; }


    public MainViewModel(INavigationService navigationService, StatusBarViewModel statusBar, ILoggingService logger, NotificationPanelViewModel notificationPanel,
        IPresetSaver presetSaver, IDispatcherService dispatcher, IUpdateCheckerService updateChecker, IApplicationState applicationState, 
        IWindowService windowService, IDialogService dialogService, ITournamentState tournamentState, ConsoleViewModel consoleViewModel, ILogStore logStore) 
        : base(dispatcher)
    {
        _updateChecker = updateChecker;
        _applicationState = applicationState;
        _windowService = windowService;
        _dialogService = dialogService;
        LogStore = logStore;
        NavigationService = navigationService;
        StatusBar = statusBar;
        Logger = logger;
        NotificationPanel = notificationPanel;
        Console = consoleViewModel;
        PresetSaver = presetSaver;
        TournamentState = tournamentState;

        NavigationService.OnSelectedViewModelChanged += UpdateDebugWindowViewModel;
        _applicationState.WindowBlockedChanged += OnWindowBlockedChanged;
        NotificationPanel.PanelOpened += OnNotificationPanelOnPanelOpened;

        SelectViewModelCommand = new RelayCommand<string>(SelectViewModel);

        VersionText = Consts.Version;
        OnPropertyChanged(nameof(VersionText));

        Task.Factory.StartNew(async () => { await CheckForUpdate(); });
    }
    public override void Dispose()
    {
        NavigationService.OnSelectedViewModelChanged -= UpdateDebugWindowViewModel;
        _applicationState.WindowBlockedChanged -= OnWindowBlockedChanged;
        NotificationPanel.PanelOpened -= OnNotificationPanelOnPanelOpened;
    }
    public override bool OnDisable()
    {
        MessageBoxResult option = _dialogService.Show("You have unsaved changes in preset.\nDo you want to save those changes?", "WARNING",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

        if (option == MessageBoxResult.Yes)
        {
            PresetSaver.SavePreset();
        } 
        else if (option == MessageBoxResult.No) { }
        else return false;
        
        return base.OnDisable();
    }

    public void OnClose()
    {
        NavigationService.SelectedView.OnDisable();
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
    
    public void UpdateDebugWindowViewModel(SelectableViewModel viewModel)
    {
        if (DebugWindowViewModel == null) return;
        DebugWindowViewModel.SelectedViewModel = viewModel;
    }
    private void OnNotificationPanelOnPanelOpened(object? o, EventArgs eventArgs)
    {
        IsHamburgerMenuOpen = false;
    }
    private void OnWindowBlockedChanged(object? sender, bool isBlocked)
    {
        IsWindowBlocked = isBlocked;
    }

    private async Task CheckForUpdate()
    {
        bool isNewUpdate = false;

        try
        {
            isNewUpdate = await _updateChecker.CheckForUpdates();
        }
        catch (Exception ex)
        {
            Logger.Warning("Problem while checking for new update: " + ex);
        }

        NewUpdate = isNewUpdate;
    }

    public void ShowUnhandledExceptionLog(string exceptionMessage)
    {
        _dialogService.Show($"Unhandled exception: {exceptionMessage}", "Application crash", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    
    public void SwitchDebugWindow()
    {
        if (!IsDebugWindowOpened)
        {
            DebugWindowViewModel = new DebugWindowViewModel(this, Dispatcher, Logger)
            {
                SelectedViewModel = NavigationService.SelectedView,
            };
            _windowService.Show(DebugWindowViewModel, null, "DebugWindow");
            _windowService.FocusMainWindow();
            IsDebugWindowOpened = true;
        }
        else
        {
            DebugWindowViewModel?.Close();
        }
    }

    public void CloseDebugWindow()
    {
        IsDebugWindowOpened = false;
        DebugWindowViewModel = null;
    }
}