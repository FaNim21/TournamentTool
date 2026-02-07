using TournamentTool.Core.Factories;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.Services;

public interface IPopupNotificationService
{
    void ShowPopup(string message, TimeSpan duration);
    void ShowPopupOnMouse(string message, TimeSpan duration);
}

public class PopupNotificationService : IPopupNotificationService
{
    private readonly IWindowService _windowService;
    private readonly IPopupViewModelFactory _popupViewModelFactory;

    //TODO: 0 Popup trzeba pojawiac na myszce
    //TODO: 0 Musi tez byc tymczasowo, czyli znikac po czasie *w pewnym sensie to jzu ejst
    //TODO: 0 Trzeba rozkminic czy tworzyc nowy popup za kazdym razem, czy wykorzystywac juz istniejacy
    

    public PopupNotificationService(IWindowService windowService, IPopupViewModelFactory popupViewModelFactory)
    {
        _windowService = windowService;
        _popupViewModelFactory = popupViewModelFactory;
    }

    public void ShowPopup(string message, TimeSpan duration)
    {
        IPopupViewModel popupViewModel = GetPopup(message, duration);
        _windowService.Show(popupViewModel, null, "PopupWindow");
        popupViewModel.Initialize();
    }
    
    public void ShowPopupOnMouse(string message, TimeSpan duration)
    {
        IPopupViewModel popupViewModel = GetPopup(message, duration);
        _windowService.ShowAtMouse(popupViewModel, null, "PopupWindow");
        popupViewModel.Initialize();
    }

    public IPopupViewModel GetPopup(string message, TimeSpan duration) => _popupViewModelFactory.Create(message, duration);
}