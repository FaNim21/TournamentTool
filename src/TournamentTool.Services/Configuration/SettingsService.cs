using System.Text.Json;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Logging;

namespace TournamentTool.Services.Configuration;

public class SettingsProvider : ISettingsProvider, ISettingsSaver
{
    private readonly IDataProtect _dataProtect;
    private readonly ILoggingService _logger;
    
    private readonly JsonSerializerOptions _serializerOptions;
    
    private readonly Dictionary<Type, object> _data = [];
    private readonly List<Action> _filesCloseActions = [];
    
    
    public SettingsProvider(IDataProtect dataProtect, ILoggingService logger)
    {
        _dataProtect = dataProtect;
        _logger = logger;
        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    public void Load()
    {
        Register(new FileStorage<Settings>("settings.json", _serializerOptions));
        Register(new FileStorage<AppCache>("app_cache.json", _serializerOptions));
        
        Register(new ProtectedFileStorage<APIKeys>("apikeys.dat", _dataProtect));
    }
    public void Save()
    {
        foreach (Action closeAction in _filesCloseActions)
        {
            closeAction.Invoke();
        }
    }
    
    internal void Register<T>(IFileStorage<T> fileStorage) where T : class, new()
    {
        T data = fileStorage.Load();

        _data[typeof(T)] = data;
        _filesCloseActions.Add(() => fileStorage.Save(Get<T>()));
    }

    public T Get<T>() where T : class
    {
        if (_data == null || _data.Count == 0)
        {
            _logger.Error($"Cannot found setting of type {typeof(T)}. This will cause problems with application");
            return null;
        }
        
        return (T)_data[typeof(T)];
    }
}

