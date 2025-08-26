using System.IO;
using System.Text.Json;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.Services;

public class SettingsService 
{
    public Settings Settings { get; private set; } = new();
    
    private readonly JsonSerializerOptions _serializerOptions;
    
    private const string _fileName = "settings.json";
    private string _path;

    
    public SettingsService()
    {
        _path = Path.Combine(Consts.AppdataPath, _fileName);
        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    public void Load()
    {
        if (!File.Exists(_path)) return;
        
        string text = File.ReadAllText(_path) ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;

        Settings? data = JsonSerializer.Deserialize<Settings>(text);
        if (data == null) return;
        Settings = data;
    }

    public void Save()
    {
        if (Settings == null) return;
        
        var data = JsonSerializer.Serialize<object>(Settings, _serializerOptions);
        File.WriteAllText(_path, data);
    }
}