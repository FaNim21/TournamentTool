using TournamentTool.Models;

namespace TournamentTool.Interfaces;

public interface IPresetSaver
{
    void SavePreset(IPreset? preset = null);
}