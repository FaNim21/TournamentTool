using System.Windows;
using System.Windows.Input;

namespace TournamentTool.ViewModels.Modals;

public class DialogBaseViewModel : BaseViewModel
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

    
    public void Close()
    {
        var activeWindow = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
        activeWindow?.Close();
    }
}
