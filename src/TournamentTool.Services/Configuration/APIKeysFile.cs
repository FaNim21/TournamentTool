using System.Text.Json;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.Configuration;

public class APIKeysFile : ISettingsFile<APIKeys>
{
    private readonly IDataProtect _dataProtect;
    
    public string FileName => "apikeys.dat";
    private readonly string _path;


    public APIKeysFile(IDataProtect dataProtect)
    {
        _dataProtect = dataProtect;
        
        _path = Path.Combine(Consts.AppdataPath, FileName);
    }
    
    public APIKeys Load()
    {
        if (!File.Exists(_path)) return new APIKeys();

        byte[] encrypted = File.ReadAllBytes(_path);
        string json = _dataProtect.UnProtect(encrypted);
        
        APIKeys? apiKeys = JsonSerializer.Deserialize<APIKeys>(json);
        return apiKeys ?? new APIKeys();
    }

    public void Save(APIKeys data)
    {
        if (data == null) return;
        
        string json = JsonSerializer.Serialize(data);
        byte[] encrypted = _dataProtect.Protect(json);
        File.WriteAllBytes(_path, encrypted);
    }
}