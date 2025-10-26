using System.Windows;
using System.Windows.Input;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities.Input;
using TournamentTool.Domain.Enums;
using TournamentTool.Services;
using ModifierKeys = System.Windows.Input.ModifierKeys;
using ModifierKeysModel = TournamentTool.Domain.Enums.ModifierKeys;

namespace TournamentTool.App.Services;

public record AppHotkey(Key key, ModifierKeys modifier);

public class InputController : IInputController, IDisposable
{
    private readonly INavigationService _navigationService;
    //TODO: 5 przekminic wiekszy sens w kwesti duplikowania keybindow w sytuacjach jezeli sa na innym selectable viewmodelu
    private Dictionary<AppHotkey, HotkeyActionType> _hotkeys { get; } = [];
    
    private readonly HashSet<Key> _pressedKeys = [];
    private readonly HashSet<Key> _previousKeys = [];
    
    public event Action<HotkeyActionType>? HotkeyPressed;

    
    public InputController(INavigationService navigationService)
    {
        _navigationService = navigationService;

        //TODO: 2 to tymczasowo, do zrobienia hotkey przez settings z zapisywaniem i wczytywaniem
        var renameTextBox = new Hotkey(KeyCode.F2, ModifierKeysModel.None, "Triggers renaming elements for now mainly in preset panel");
        var toggleHamburgerMenu = new Hotkey(KeyCode.F1, ModifierKeysModel.None, "Toggle visibility for hamburger menu");
        var toggleStudioMode = new Hotkey(KeyCode.S, ModifierKeysModel.Shift, "Toggle Studio Mode in controller panel");
        var toggleDebugWindow = new Hotkey(KeyCode.F12, ModifierKeysModel.None, "Toggle mode for debug window for specific selected view model");
        var savePreset = new Hotkey(KeyCode.S, ModifierKeysModel.Ctrl, "Saves current preset changes");

        AddHotkey(renameTextBox, HotkeyActionType.General_RenameElementOnMousePosition);
        AddHotkey(toggleHamburgerMenu, HotkeyActionType.General_ToggleHamburgerMenu);
        AddHotkey(toggleStudioMode, HotkeyActionType.Controller_ToggleStudioMode);
        AddHotkey(toggleDebugWindow, HotkeyActionType.General_ToggleDebugWindow);
        AddHotkey(savePreset, HotkeyActionType.General_SavePreset);
        
        Application.Current.MainWindow!.KeyDown += HandleKeyDown;
        Application.Current.MainWindow.KeyUp += HandleKeyUp;
        HotkeyPressed += HandleNavigationHotkeys;
    }
    public void Dispose()
    {
        if (Application.Current.MainWindow == null) return;
        
        Application.Current.MainWindow.KeyDown -= HandleKeyDown;
        Application.Current.MainWindow.KeyUp -= HandleKeyUp;
        HotkeyPressed -= HandleNavigationHotkeys;
    }

    private void HandleNavigationHotkeys(HotkeyActionType actionType)
    {
        if (_navigationService.SelectedView is not IHotkeyReceiver hotkeyReceiver) return;
        hotkeyReceiver.OnHotkey(actionType);
    }
    
    public void InitializeWindow(object sender)
    {
        if (sender is not Window window) return;   

        window.KeyDown += HandleKeyDown;
        window.KeyUp += HandleKeyUp;
    }
    public void CleanupWindow(object sender)
    {
        if (sender is not Window window) return;   

        window.KeyDown -= HandleKeyDown;
        window.KeyUp -= HandleKeyUp;
    }

    private void HandleKeyDown(object sender, KeyEventArgs e)
    {
        _pressedKeys.Add(e.Key);
        CheckHotkeys();
    }
    private void HandleKeyUp(object sender, KeyEventArgs e)
    {
        _pressedKeys.Remove(e.Key);
    }

    private void CheckHotkeys()
    {
        //TODO: 0 problem z tym ze ctrl+s nie przechwyci, bo rejestruje ctrl jako pressed keys i wtedy count jest == 2 z wcisnietym pozniej s
        if (_pressedKeys.Count != 1) return;

        Key pressedKey = _pressedKeys.FirstOrDefault();
        ModifierKeys modifiers = Keyboard.Modifiers;
        if (pressedKey == Key.None) return;
        
        AppHotkey pressedHotkey = new AppHotkey(pressedKey, modifiers);
        if (!_hotkeys.TryGetValue(pressedHotkey, out HotkeyActionType value)) return;
        
        HotkeyPressed?.Invoke(value);
    }
    
    public void AddHotkey(Hotkey hotkey, HotkeyActionType actionType)
    {
        Key key = Enum.Parse<Key>(hotkey.Key.ToString());
        ModifierKeys modifiers = ConvertModifiers(hotkey.Modifiers);
        
        AppHotkey appHotkey = new AppHotkey(key, modifiers);
        _hotkeys.Add(appHotkey, actionType);
    }
    public void RemoveHotkey(Hotkey hotkey)
    {
        //TODO: 5 Usuniecie hotkey
    }
    public void ChangeHotkey(Hotkey hotkey)
    {
        //TODO: 5 Zmiana hotkey
    }
    
    private ModifierKeys ConvertModifiers(ModifierKeysModel model)
    {
        ModifierKeys result = ModifierKeys.None;

        if (model.HasFlag(ModifierKeysModel.Ctrl))
            result |= ModifierKeys.Control;
        if (model.HasFlag(ModifierKeysModel.Alt))
            result |= ModifierKeys.Alt;
        if (model.HasFlag(ModifierKeysModel.Shift))
            result |= ModifierKeys.Shift;
        if (model.HasFlag(ModifierKeysModel.Super))
            result |= ModifierKeys.Windows;

        return result;
    }
}