using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.ViewModels.Commands.DialogBox;

namespace TournamentTool.ViewModels.Modals;

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

    public DialogBoxViewModel(IDispatcherService dispatcher) : base(dispatcher)
    {
        ButtonPress = new DialogBoxButtonClickCommand(this);
    }
    ~DialogBoxViewModel()
    {
        _buttons = [];
    }
}
