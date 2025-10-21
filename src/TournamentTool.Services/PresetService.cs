using System.Text.Json;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services;

public class PresetService : IPresetSaver
{
    public ITournamentPresetManager Tournament { get; set; }
    
    private readonly JsonSerializerOptions _serializerOptions;


    public PresetService(ITournamentPresetManager tournament)
    {
        Tournament = tournament;
        
        _serializerOptions = new JsonSerializerOptions() { WriteIndented = true };
    }
    
    
    public void SavePreset(IPreset? preset)
    {
        if (preset == null) return;

        var data = JsonSerializer.Serialize<object>(preset, _serializerOptions);
        string path = preset.GetPath(Consts.AppdataPath);
        File.WriteAllText(path, data);
    }

    public void SavePreset()
    {
        if (Tournament.IsNullOrEmpty()) return;
        
        IPreset preset = Tournament.GetData();
        var data = JsonSerializer.Serialize<object>(preset, _serializerOptions);
        string path = preset.GetPath(Consts.AppdataPath);
        File.WriteAllText(path, data);
        Tournament.PresetIsSaved();
    }
}