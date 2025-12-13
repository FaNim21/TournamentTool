using System.Text.Json;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;

namespace TournamentTool.Services;

public class SettingsService : ISettings, ISettingsSaver
{
    private readonly IDataProtect _dataProtect;

    public Settings Settings { get; private set; } = new();
    public APIKeys APIKeys { get; private set; } = new();
    
    private readonly JsonSerializerOptions _serializerOptions;
    
    private const string _settingsFileName = "settings.json";
    private const string _apiKeysFileName = "apikeys.dat";
    private string _settingsPath;
    private string _apiKeysPath;

    
    public SettingsService(IDataProtect dataProtect)
    {
        _dataProtect = dataProtect;

        _settingsPath = Path.Combine(Consts.AppdataPath, _settingsFileName);
        _apiKeysPath = Path.Combine(Consts.AppdataPath, _apiKeysFileName);
        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    public bool Load()
    {
        LoadAPIKeys();
        
        if (!File.Exists(_settingsPath)) return false;
        
        string text = File.ReadAllText(_settingsPath) ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return false;

        Settings? data = JsonSerializer.Deserialize<Settings>(text);
        if (data == null) return false;
        Settings = data;
        
        return true;
    }
    public void Save()
    {
        SaveAPIKeys();
        
        if (Settings == null) return;
        
        var data = JsonSerializer.Serialize<object>(Settings, _serializerOptions);
        File.WriteAllText(_settingsPath, data);
    }

    private void LoadAPIKeys()
    {
        if (!File.Exists(_apiKeysPath)) return;

        byte[] encrypted = File.ReadAllBytes(_apiKeysPath);
        string json = _dataProtect.UnProtect(encrypted);
        APIKeys? apiKeys = JsonSerializer.Deserialize<APIKeys>(json);
        
        if (apiKeys == null) return;
        APIKeys = apiKeys;
    }
    private void SaveAPIKeys()
    {
        if (APIKeys == null) return;
        
        string json = JsonSerializer.Serialize(APIKeys);
        byte[] encrypted = _dataProtect.Protect(json);
        File.WriteAllBytes(_apiKeysPath, encrypted);
    }
}

