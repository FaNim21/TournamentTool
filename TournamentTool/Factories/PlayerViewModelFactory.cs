using TournamentTool.Models;
using TournamentTool.Services;
using TournamentTool.Services.External;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Factories;

public interface IPlayerViewModelFactory
{
    PlayerViewModel Create(Player? player = null);
}

public class PlayerViewModelFactory : IPlayerViewModelFactory
{
    private readonly IMinecraftDataService _minecraftDataService;


    public PlayerViewModelFactory(IMinecraftDataService minecraftDataService)
    {
        _minecraftDataService = minecraftDataService;
    }

    public PlayerViewModel Create(Player? player = null)
    {
        return new PlayerViewModel(_minecraftDataService, player);
    }
}