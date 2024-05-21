using System.Windows;
using TournamentTool.Commands.DialogBoxCommands;
using TournamentTool.Components.Controls;

namespace TournamentTool.ViewModels.DialogBoxViewModels;

public class DialogBoxViewModel : DialogBaseViewModel
{
    private MessageBoxImage _icon;
    public MessageBoxImage Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            OnPropertyChanged(nameof(Icon));
        }
    }

    private IEnumerable<DialogBoxButton> _buttons = new List<DialogBoxButton>();
    public IEnumerable<DialogBoxButton> Buttons
    {
        get => _buttons;
        init
        {
            _buttons = value;
            OnPropertyChanged(nameof(Buttons));
        }
    }

    public DialogBoxViewModel()
    {
        ButtonPress = new DialogBoxButtonClickCommand(this);
    }
    ~DialogBoxViewModel()
    {
        _buttons = Enumerable.Empty<DialogBoxButton>();
    }
}
