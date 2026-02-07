using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.ViewModels.Modals;

public class PopupWindowViewModel : BaseWindowViewModel, IPopupViewModel
{
    private string _text = string.Empty;
    public string Text
    {
        get => _text;
        private set
        {
            _text = value;
            OnPropertyChanged(nameof(Text));
        }
    }

    private TimeSpan _duration;
    
    
    public PopupWindowViewModel(string text, TimeSpan duration, IDispatcherService dispatcher) : base(dispatcher)
    {
        Text = text;
        _duration = duration;

        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        await Task.Delay(_duration);
        Close();
    }
}