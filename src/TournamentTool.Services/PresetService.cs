using System.Text.Json;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services;

public class PresetService : IPresetSaver
{
    private readonly ITournamentState _tournamentState;
    
    private readonly JsonSerializerOptions _serializerOptions;


    public PresetService(ITournamentState tournamentState)
    {
        _tournamentState = tournamentState;
        
        _serializerOptions = new JsonSerializerOptions() { WriteIndented = true };
    }
    
    
    public void SavePreset(IPreset? preset)
    {
        if (preset == null) return;

        var data = JsonSerializer.Serialize<object>(preset, _serializerOptions);
        string path = preset.GetPath(Consts.PresetsPath);
        
        File.WriteAllText(path, data);
    }

    public void SavePreset()
    {
        if (_tournamentState.CurrentPreset == null || string.IsNullOrEmpty(_tournamentState.CurrentPreset.Name)) return;
        
        IPreset preset = _tournamentState.CurrentPreset;
        var data = JsonSerializer.Serialize<object>(preset, _serializerOptions);
        string path = preset.GetPath(Consts.PresetsPath);
        File.WriteAllText(path, data);
        _tournamentState.MarkAsUnmodified();
    }
}