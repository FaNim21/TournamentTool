using TournamentTool.Core.Interfaces;
using TournamentTool.Services.External;

namespace TournamentTool.ViewModels.Entities.Player;


public class PlayerViewModelFactory : IPlayerViewModelFactory
{
    private readonly IMinecraftDataService _minecraftDataService;
    private readonly IImageService _imageService;
    private readonly IDispatcherService _dispatcher;
    private readonly IDialogService _dialogService;


    public PlayerViewModelFactory(IMinecraftDataService minecraftDataService, IImageService imageService, IDispatcherService dispatcher, IDialogService dialogService)
    {
        _minecraftDataService = minecraftDataService;
        _imageService = imageService;
        _dispatcher = dispatcher;
        _dialogService = dialogService;
    }

    public IPlayerViewModel Create(Domain.Entities.Player? player = null)
    {
        return new PlayerViewModel(_minecraftDataService, _imageService, _dispatcher, _dialogService, player);
    }
}