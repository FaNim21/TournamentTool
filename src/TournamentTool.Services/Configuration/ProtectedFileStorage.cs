using System.Text.Json;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;

namespace TournamentTool.Services.Configuration;

public class ProtectedFileStorage<T> : IFileStorage<T> where T : class, new()
{
    private readonly IDataProtect _dataProtect;
    private readonly string _path;


    public ProtectedFileStorage(string fileName, IDataProtect dataProtect)
    {
        _dataProtect = dataProtect;
        
        _path = Path.Combine(Consts.AppdataPath, fileName);
    }
    
    public T Load()
    {
        if (!File.Exists(_path)) return new T();

        byte[] encrypted = File.ReadAllBytes(_path);
        string json = _dataProtect.UnProtect(encrypted);
        
        T? data = JsonSerializer.Deserialize<T>(json);
        return data ?? new T();
    }

    public void Save(T data)
    {
        if (data == null) return;
        
        string json = JsonSerializer.Serialize(data);
        byte[] encrypted = _dataProtect.Protect(json);
        File.WriteAllBytes(_path, encrypted);
    }
}