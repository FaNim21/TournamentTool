using TournamentTool.App.Components;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels;

namespace TournamentTool.App.Services;

public interface IApplicationLifetime
{
    void OnStartup();
    void OnExit();
}

public class ApplicationLifetime : IApplicationLifetime
{
    private readonly MainViewModel _mainViewModel;
    private readonly IObsController _obsController;
    
    private readonly IPresetSaver _presetSaver;
    private readonly IInputController _inputController;
    private readonly ISettingsSaver _settingsSaver;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IWindowService _windowService;
    private readonly ITwitchService _twitchService;
    private readonly ILoggingService _logger;
    private readonly ILogStore _logStore;


    public ApplicationLifetime(MainViewModel mainViewModel, IPresetSaver presetSaver, IInputController inputController, ISettingsSaver settingsSaver, 
        ISettingsProvider settingsProvider, IObsController obsController, IWindowService windowService, ITwitchService twitchService, ILoggingService logger,
        ILogStore logStore)
    {
        _mainViewModel = mainViewModel;
        _presetSaver = presetSaver;
        _inputController = inputController;
        _settingsSaver = settingsSaver;
        _settingsProvider = settingsProvider;
        _obsController = obsController;
        _windowService = windowService;
        _twitchService = twitchService;
        _logger = logger;
        _logStore = logStore;
    }
    
    /// <summary>
    /// In future async because of more logic on startup and potential startup loading window for that?
    /// </summary>
    public void OnStartup()
    {
        Settings settings = _settingsProvider.Get<Settings>();
        APIKeys apiKeys = _settingsProvider.Get<APIKeys>();
        
        _windowService.SetMainWindowTopMost(settings.IsAlwaysOnTop);

        if (settings is { SaveTwitchToken: true, AutoLoginToTwitch: true } && !string.IsNullOrEmpty(apiKeys.TwitchAccessToken))
        {
            _twitchService.ConnectAsync();
        }
        
        _inputController.HotkeyPressed += HandleGeneralHotkeys;
        
        Task.Run(_obsController.Connect);
    }

    public void OnExit()
    {
        _inputController.HotkeyPressed -= HandleGeneralHotkeys;
        
        Settings settings = _settingsProvider.Get<Settings>();
        if (settings.SaveLogsAfterShutdown)
        {
            Task.Run(async () => { await _logStore.SaveToFileAsync();});
        }
        
        _settingsSaver.Save();
    }

    private void HandleGeneralHotkeys(HotkeyActionType actionType)
    {
        switch (actionType)
        {
            case HotkeyActionType.General_SavePreset: 
                _presetSaver.SavePreset();
                break;
            case HotkeyActionType.General_RenameElementOnMousePosition: 
                var textBlock = UIHelper.GetFocusedUIElement<EditableTextBlock>();
                if (textBlock is { IsEditable: true })
                {
                    textBlock.IsInEditMode = true;
                }
                break;
            case HotkeyActionType.General_ToggleDebugWindow:
                _mainViewModel.SwitchDebugWindow();
                break;
            case HotkeyActionType.General_ToggleHamburgerMenu: 
                _mainViewModel.IsHamburgerMenuOpen = !_mainViewModel.IsHamburgerMenuOpen;
                break;
            case HotkeyActionType.General_ToggleConsole:
                _mainViewModel.Console.Toggle();
                break;
        }
    }
}