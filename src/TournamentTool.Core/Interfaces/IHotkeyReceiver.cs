using TournamentTool.Domain.Enums;

namespace TournamentTool.Core.Interfaces;

public interface IHotkeyReceiver
{
    void OnHotkey(HotkeyActionType actionType);
}