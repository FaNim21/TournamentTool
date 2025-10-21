using TournamentTool.Domain.Entities;

namespace TournamentTool.Core.Interfaces;

public interface IPlayerViewModelFactory
{
    public IPlayerViewModel Create(Player? player = null);
}