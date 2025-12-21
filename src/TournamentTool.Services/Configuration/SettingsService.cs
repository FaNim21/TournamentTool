using System.Text.Json;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Logging;

namespace TournamentTool.Services.Configuration;

public class SettingsProviderService : ISettingsProvider, ISettingsSaver
{
    private readonly IDataProtect _dataProtect;
    private readonly ILoggingService _logger;
    
    private readonly JsonSerializerOptions _serializerOptions;
    
    private readonly Dictionary<Type, object> _data = [];
    private readonly List<Action> _filesCloseActions = [];

    public Settings Settings { get; private set; } = new();
    public APIKeys APIKeys { get; private set; } = new();
    
    
    public SettingsProviderService(IDataProtect dataProtect, ILoggingService logger)
    {
        _dataProtect = dataProtect;
        _logger = logger;
        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    public void Load()
    {
        Register(new AppSettingsFile(_serializerOptions));
        Register(new APIKeysFile(_dataProtect));
    }
    public void Save()
    {
        foreach (Action closeAction in _filesCloseActions)
        {
            closeAction.Invoke();
        }
    }
    
    internal void Register<T>(ISettingsFile<T> settingsFile) where T : class, new()
    {
        T data = settingsFile.Load();

        _data[typeof(T)] = data;
        _filesCloseActions.Add(() => settingsFile.Save(Get<T>()));
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

