using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;

namespace TournamentTool.ViewModels.Modals;

public class DialogBaseViewModel : BaseViewModel
{
    public IWindowService WindowService { get; set; }
    private string? _text;

    public string? Text
    {
        get => _text;
        set
        {
            _text = value;
            OnPropertyChanged(nameof(Text));
        }
    }

    private readonly string? _caption;
    public string? Caption
    {
        get => _caption;
        init
        {
            _caption = value;
            OnPropertyChanged(nameof(Caption));
        }
    }

    private MessageBoxResult _result = MessageBoxResult.None;
    public MessageBoxResult Result
    {
        get => _result;
        set
        {
            _result = value;
            OnPropertyChanged(nameof(Result));
        }
    }

    public ICommand? ButtonPress { get; set; }


    protected DialogBaseViewModel(IWindowService windowService, IDispatcherService dispatcher) : base(dispatcher)
    {
        WindowService = windowService;
    }
    public void Close()
    {
        WindowService.Close<DialogBaseViewModel>();
    }
}
