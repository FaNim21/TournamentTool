using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.ViewModels.Modals;

public class PopupWindowViewModel : BaseWindowViewModel, IPopupViewModel
{
    private string _text = string.Empty;
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            OnPropertyChanged(nameof(Text));
        }
    }

    private TimeSpan _duration;
    
    //TODO: 0 Tutaj kalkulowac rozmiar popupu zaleznie od tekstu ustalajac max width i height z text wrap
    
    public PopupWindowViewModel(string text, TimeSpan duration, IDispatcherService dispatcher) : base(dispatcher)
    {
        Text = text;
        _duration = duration;
    }

    public void Initialize()
    {
        //TODO: 0 Zrobic zatrzymywanie taska, zaleznie od tego czy tworzymy nowy czy 
        Task.Factory.StartNew(async ()=> await InitializeAsync());
    }

    private async Task InitializeAsync()
    {
        await Task.Delay(_duration);
        Close();
    }
}