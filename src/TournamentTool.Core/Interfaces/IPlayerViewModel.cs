using TournamentTool.Domain.Entities;

namespace TournamentTool.Core.Interfaces;

public interface IPlayerViewModel
{
    Player Data { get; }

    void UpdateHeadImage();
    void SetStreamName(string name);
}