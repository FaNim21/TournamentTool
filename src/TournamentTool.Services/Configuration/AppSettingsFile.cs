using System.Text.Json;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.Configuration;

public class AppSettingsFile : ISettingsFile<Settings>
{
    private readonly JsonSerializerOptions _options;
    
    public string FileName => "settings.json";
    private readonly string _path;


    public AppSettingsFile(JsonSerializerOptions options)
    {
        _options = options;
        
        _path = Path.Combine(Consts.AppdataPath, FileName);
    }

    public Settings Load()
    {
        if (!File.Exists(_path)) return new Settings();
        
        string text = File.ReadAllText(_path) ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return new Settings();

        Settings? settings = JsonSerializer.Deserialize<Settings>(text);
        return settings ?? new Settings();
    }

    public void Save(Settings data)
    {
        if (data == null) return;
        
        string jsonData = JsonSerializer.Serialize<object>(data, _options);
        File.WriteAllText(_path, jsonData);
    }
}