using TournamentTool.Domain.Enums;

namespace TournamentTool.Core.Interfaces;

public interface IDialogService
{
    MessageBoxResult Show(string text, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None);

    public string ShowOpenFile(string? filter = null);
    public string ShowOpenFolder();
}