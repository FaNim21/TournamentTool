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
        _bindingEngine.RegisterSchema(BindingSchema.CreatePOV("head"));
        _bindingEngine.RegisterSchema(BindingSchema.CreatePOV("display_name"));
        _bindingEngine.RegisterSchema(BindingSchema.CreatePOV("ign"));
        _bindingEngine.RegisterSchema(BindingSchema.CreatePOV("pb"));
        _bindingEngine.RegisterSchema(BindingSchema.CreatePOV("team_name"));
        _bindingEngine.RegisterSchema(BindingSchema.CreatePOV("stream_name"));
        _bindingEngine.RegisterSchema(BindingSchema.CreatePOV("stream_type"));

        _bindingEngine.RegisterSchema(BindingSchema.CreateRankedManagement("custom_text"));
        _bindingEngine.RegisterSchema(BindingSchema.CreateRankedManagement("rounds"));
        _bindingEngine.RegisterSchema(BindingSchema.CreateRankedManagement("completions"));
        _bindingEngine.RegisterSchema(BindingSchema.CreateRankedManagement("players"));
    }
    
    private void LoadAppCacheBindings()
    {
        foreach (KeyValuePair<string, SceneItemConfiguration> config in _appCache.SceneItemConfigs)
        {
            _bindingEngine.GetOrCreateNode(config.Value.BindingKey);
        }
    }
}
