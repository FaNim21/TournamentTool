using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.Services;

public class SettingsService : ISettings, ISettingsSaver
{
    public Settings Settings { get; private set; } = new();
    public APIKeys APIKeys { get; private set; } = new();
    
    private readonly JsonSerializerOptions _serializerOptions;
    
    private const string _settingsFileName = "settings.json";
    private const string _apiKeysFileName = "apikeys.dat";
    private string _settingsPath;
    private string _apiKeysPath;

    
    public SettingsService()
    {
        _settingsPath = Path.Combine(Consts.AppdataPath, _settingsFileName);
        _apiKeysPath = Path.Combine(Consts.AppdataPath, _apiKeysFileName);
        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };
        
    }

    public void Load()
    {
        LoadAPIKeys();
        
        if (!File.Exists(_settingsPath)) return;
        
        string text = File.ReadAllText(_settingsPath) ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;

        Settings? data = JsonSerializer.Deserialize<Settings>(text);
        if (data == null) return;
        Settings = data;
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
        byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        string json = Encoding.UTF8.GetString(decrypted);

        APIKeys? apiKeys = JsonSerializer.Deserialize<APIKeys>(json);
        if (apiKeys == null) return;
        APIKeys = apiKeys;
        
        Array.Clear(decrypted, 0, decrypted.Length);
    }
    private void SaveAPIKeys()
    {
        if (APIKeys == null) return;
        
        string json = JsonSerializer.Serialize(APIKeys);
        byte[] raw = Encoding.UTF8.GetBytes(json);

        byte[] encrypted = ProtectedData.Protect(raw, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_apiKeysPath, encrypted);}
}

