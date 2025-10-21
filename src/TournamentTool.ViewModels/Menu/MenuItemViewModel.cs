using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.ViewModels.Menu;

public class MenuItemViewModel : BaseViewModel
{
    public ObservableCollection<MenuItemViewModel> Children { get; } = [];
    
    private string _header = string.Empty;
    public string Header
    {
        get => _header;
        set
        {
            _header = value;
            OnPropertyChanged(nameof(Header));
        }
    }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            OnPropertyChanged(nameof(IsEnabled));
        }
    }

    private bool _isSeparator;
    public bool IsSeparator
    {
        get => _isSeparator;
        set
        {
            _isSeparator = value;
            OnPropertyChanged(nameof(IsSeparator));
        }
    }
    
    private ICommand? _command;
    public ICommand? Command
    {
        get => _command;
        set
        {
            _command = value;
            OnPropertyChanged(nameof(Command));
        }
    }


    public MenuItemViewModel(IDispatcherService dispatcher) : base(dispatcher) { }
}