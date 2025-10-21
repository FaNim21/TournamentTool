namespace TournamentTool.ViewModels.Menu;

public interface IMenuService
{
    void ShowContextMenu(ContextMenuViewModel menuViewModel, object? placementTarget = null);
    void ShowContextMenu(IContextMenuBuilder menuBuilder, object? placementTarget = null);
}