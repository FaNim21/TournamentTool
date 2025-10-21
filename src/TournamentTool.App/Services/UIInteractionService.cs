using TournamentTool.App.Components;
using TournamentTool.App.Components.Behaviors;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.App.Services;

public class UIInteractionService : IUIInteractionService
{
    public void EnterEditModeOnHoverTextBlock()
    {
        EditableTextBlock? textBlock = UIHelper.GetFocusedUIElement<EditableTextBlock>();
        if (textBlock is { IsEditable: true })
            textBlock.IsInEditMode = true;
    }
}