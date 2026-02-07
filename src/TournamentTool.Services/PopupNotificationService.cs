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


    public PopupNotificationService(IWindowService windowService, IPopupViewModelFactory popupViewModelFactory)
    {
        _windowService = windowService;
        _popupViewModelFactory = popupViewModelFactory;
    }

    public void ShowPopup(string message, TimeSpan duration)
    {
        IPopupViewModel popupViewModel = GetPopup(message, duration);
        _windowService.Show(popupViewModel, null, "PopupWindow");
    }
    
    public void ShowPopupOnMouse(string message, TimeSpan duration)
    {
        IPopupViewModel popupViewModel = GetPopup(message, duration);
        _windowService.ShowAtMouse(popupViewModel, null, "PopupWindow");
    }

    public IPopupViewModel GetPopup(string message, TimeSpan duration) => _popupViewModelFactory.Create(message, duration);
}