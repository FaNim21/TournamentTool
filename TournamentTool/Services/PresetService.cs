using System.IO;
using System.Text.Json;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Services;

public class PresetService : IPresetSaver
{
    public TournamentViewModel Tournament { get; set; }
    
    private readonly JsonSerializerOptions _serializerOptions;


    public PresetService(TournamentViewModel tournament)
    {
        Tournament = tournament;
        
        _serializerOptions = new JsonSerializerOptions() { WriteIndented = true };
    }
    
    
    public void SavePreset(IPreset? preset)
    {
        if (preset == null) return;

        var data = JsonSerializer.Serialize<object>(preset, _serializerOptions);
        string path = preset.GetPath();
        File.WriteAllText(path, data);
    }

    public void SavePreset()
    {
        if (Tournament.IsNullOrEmpty()) return;
        
        IPreset preset = Tournament.GetData();
        var data = JsonSerializer.Serialize<object>(preset, _serializerOptions);
        string path = preset.GetPath();
        File.WriteAllText(path, data);
    }
}