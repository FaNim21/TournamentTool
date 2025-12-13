using TournamentTool.App.Components;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Controllers;
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
    private readonly ObsController _obsController;
    
    private readonly IPresetSaver _presetSaver;
    private readonly IInputController _inputController;
    private readonly ISettingsSaver _settingsSaver;
    private readonly ISettings _settingsService;
    private readonly IWindowService _windowService;
    private readonly ITwitchService _twitchService;


    public ApplicationLifetime(MainViewModel mainViewModel, IPresetSaver presetSaver, IInputController inputController, ISettingsSaver settingsSaver, 
        ISettings settingsService, ObsController obsController, IWindowService windowService, ITwitchService twitchService)
    {
        _mainViewModel = mainViewModel;
        _presetSaver = presetSaver;
        _inputController = inputController;
        _settingsSaver = settingsSaver;
        _settingsService = settingsService;
        _obsController = obsController;
        _windowService = windowService;
        _twitchService = twitchService;
    }
    
    /// <summary>
    /// In future async because of more logic on startup and potential startup loading window for that?
    /// </summary>
    public void OnStartup()
    {
        bool success = _settingsSaver.Load();
        if (success)
        {
            _windowService.SetMainWindowTopMost(_settingsService.Settings.IsAlwaysOnTop);

            if (_settingsService.Settings is { SaveTwitchToken: true, AutoLoginToTwitch: true } && !string.IsNullOrEmpty(_settingsService.APIKeys.TwitchAccessToken))
            {
                _twitchService.ConnectAsync();
            }
        }
        
        _inputController.HotkeyPressed += HandleGeneralHotkeys;
        
        Task.Run(_obsController.Connect);
    }

    public void OnExit()
    {
        _inputController.HotkeyPressed -= HandleGeneralHotkeys;
        
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
        }
    }
}