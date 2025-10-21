using TournamentTool.Domain.Entities;

namespace TournamentTool.Domain.Interfaces;

public interface IPresetSaver
{
    void SavePreset(IPreset? preset);
    void SavePreset();
}