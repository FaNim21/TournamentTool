using System.Text.Json;
using TournamentTool.Core.Utils;

namespace TournamentTool.Services.Configuration;

public class FileStorage<T> : IFileStorage<T> where T : class, new()
{
    private readonly JsonSerializerOptions _options;
    private readonly string _path;


    public FileStorage(string fileName, JsonSerializerOptions options)
    {
        _options = options;
        
        _path = Path.Combine(Consts.AppdataPath, fileName);
    }

    public T Load()
    {
        if (!File.Exists(_path)) return new T();
        
        string text = File.ReadAllText(_path) ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return new T();

        T? data = JsonSerializer.Deserialize<T>(text);
        return data ?? new T();
    }

    public void Save(T data)
    {
        if (data == null) return;
        
        string jsonData = JsonSerializer.Serialize<object>(data, _options);
        File.WriteAllText(_path, jsonData);
    }
}