using TournamentTool.Domain.Entities.Input;
using TournamentTool.Domain.Enums;

namespace TournamentTool.Core.Interfaces;

public interface IInputController
{
    event Action<HotkeyActionType>? HotkeyPressed;
    
    void InitializeWindow(object window);
    void CleanupWindow(object window);

    void AddHotkey(Hotkey hotkey, HotkeyActionType actionType);
    void RemoveHotkey(Hotkey hotkey);
    void ChangeHotkey(Hotkey hotkey);
}