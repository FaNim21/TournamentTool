using System.Windows;
using System.Windows.Input;

namespace TournamentTool.Utils;

public class Hotkey
{
    public Key Key { get; init; }
    public ModifierKeys ModifierKeys { get; init; }
    public string? Description { get; set; }
    public Action? Action { get; set; }
}

public class InputController
{
    public static InputController Instance => instance;
    public event EventHandler? HotkeyPressed;

    private static readonly InputController instance = new();
    private readonly HashSet<Key> _pressedKeys = [];
    private readonly HashSet<Key> _previousKeys = [];
    private readonly List<Hotkey> _hotkeys = [];


    static InputController() { }
    private InputController() { }

    public void Initialize()
    {
        Application.Current.MainWindow!.KeyDown += HandleKeyDown;
        Application.Current.MainWindow.KeyUp += HandleKeyUp;
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

    public bool GetKey(Key key)
    {
        return _pressedKeys.Contains(key);
    }
    public bool GetKeyDown(Key key)
    {
        return _pressedKeys.Contains(key) && !_previousKeys.Contains(key);
    }

    public void Cleanup()
    {
        Application.Current.MainWindow!.KeyDown -= HandleKeyDown;
        Application.Current.MainWindow.KeyUp -= HandleKeyUp;
        HotkeyPressed = null;

        for (int i = 0; i < _hotkeys.Count; i++)
            _hotkeys[i].Action = null;
    }

    private void CheckHotkeys()
    {
        foreach (var hotkey in _hotkeys)
        {
            if (IsHotkeyPressed(hotkey))
            {
                hotkey.Action?.Invoke();
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private bool IsHotkeyPressed(Hotkey hotkey)
    {
        return hotkey.ModifierKeys == Keyboard.Modifiers && _pressedKeys.Contains(hotkey.Key);
    }

    public void AddHotkey(Hotkey hotkey)
    {
        _hotkeys.Add(hotkey);
    }
    public void RemoveHotkey(Hotkey hotkey)
    {
        _hotkeys.Remove(hotkey);
    }
    public void ChangeHotkey(Hotkey hotkey)
    {
        //TODO: 0 Zmiana hotkey
    }

    public List<Hotkey> GetHotkeys()
    {
        return _hotkeys;
    }
}
