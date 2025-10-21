using TournamentTool.Domain.Enums;

namespace TournamentTool.ViewModels.Modals;

public class DialogBoxButton
{
    public string? Title { get; set; }

    public MessageBoxResult Result { get; set; }
}
