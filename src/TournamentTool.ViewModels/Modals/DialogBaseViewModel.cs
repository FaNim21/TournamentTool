using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;

namespace TournamentTool.ViewModels.Modals;

public class DialogBaseViewModel : BaseWindowViewModel
{
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


    protected DialogBaseViewModel(IDispatcherService dispatcher) : base(dispatcher) { }
    public void Close()
    {
        RequestClose?.Invoke();
    }
}
