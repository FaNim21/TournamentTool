using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;

namespace TournamentTool.Core.Factories;

public interface IPlayerViewModelFactory
{
    public IPlayerViewModel Create(Player? player = null);
}