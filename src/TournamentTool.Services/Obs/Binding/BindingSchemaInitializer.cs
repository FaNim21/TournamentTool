using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Domain.Obs;

namespace TournamentTool.Services.Obs.Binding;

public class BindingSchemaInitializer : IBindingSchemaInitializer
{
    private readonly IBindingEngine _bindingEngine;

    private readonly AppCache _appCache;
    
    public BindingSchemaInitializer(IBindingEngine bindingEngine, ISettingsProvider settingsProvider)
    {
        _bindingEngine = bindingEngine;

        _appCache = settingsProvider.Get<AppCache>();
    }
    
    public void Initialize()
    {
        InitializePointOfViewSchemes();

        LoadAppCacheBindings();
    }

    private void InitializePointOfViewSchemes()
    {
        //POV
        _bindingEngine.RegisterSchema(BindingSchema.New("POV", "head", true));
        _bindingEngine.RegisterSchema(BindingSchema.New("POV", "display_name", true));
        _bindingEngine.RegisterSchema(BindingSchema.New("POV", "ign", true));
        _bindingEngine.RegisterSchema(BindingSchema.New("POV", "pb", true));
        _bindingEngine.RegisterSchema(BindingSchema.New("POV", "team_name", true));
        _bindingEngine.RegisterSchema(BindingSchema.New("POV", "stream_name", true));
        _bindingEngine.RegisterSchema(BindingSchema.New("POV", "stream_type", true));
    }
    
    private void LoadAppCacheBindings()
    {
        foreach (KeyValuePair<string, SceneItemConfiguration> config in _appCache.SceneItemConfigs)
        {
            _bindingEngine.UpsertItem(config.Key, config.Value.BindingKey);
        }
        
        //TODO: 0 Ladowac wszystkie configi z appcache do binding engine
        // _bindingEngine.
    }
}
