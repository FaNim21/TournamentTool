using TournamentTool.Core.Factories;
using TournamentTool.Core.Interfaces;
using TournamentTool.ViewModels.Modals;

namespace TournamentTool.ViewModels.Factories;

public class PopupViewModelFactory : IPopupViewModelFactory
{
    private readonly IDispatcherService _dispatcherService;

    
    public PopupViewModelFactory(IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService;
    }
    
    public IPopupViewModel Create(string text, TimeSpan duration)
    {
        return new PopupWindowViewModel(text, duration, _dispatcherService);
    }
}