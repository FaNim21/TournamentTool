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
    
    //TODO: 9 Pomsyly na przyszlosc odnosnie popup'ow
    //- Dac opcje klikniecia na nie zeby je zamknac
    //- Zrobic prosta animacje przy zamykaniu, wykorzystujaca metode close do zamykania i tak,
    //wiec mozna tu spokojnie ze strony kodu zrobic animacje zmieniajaca opacity
    
    
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