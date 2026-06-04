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
        InitializeSchemas();
        InitializeSubSchemas();
        InitializeConnections();

        LoadAppCachedBindings();
    }
    
    private void InitializeSchemas()
    {
        //POV
        _bindingEngine.RegisterSchema(BindingSchema.CreatePOV(string.Empty));   //Nie ma obecnie unikatowych dla siebie zadnych wartosci
        
        //Ranked Management data
        _bindingEngine.RegisterSchema(BindingSchema.CreateRankedManagement(nameof(RankedManagementData.CustomText)));
        _bindingEngine.RegisterSchema(BindingSchema.CreateRankedManagement(nameof(RankedManagementData.Rounds)));
        _bindingEngine.RegisterSchema(BindingSchema.CreateRankedManagement(nameof(RankedManagementData.Completions)));
        _bindingEngine.RegisterSchema(BindingSchema.CreateRankedManagement(nameof(RankedManagementData.Players)));

        //Leaderboard
        _bindingEngine.RegisterSchema(BindingSchema.CreateLeaderboard("points"));
        _bindingEngine.RegisterSchema(BindingSchema.CreateLeaderboard("position"));
        _bindingEngine.RegisterSchema(BindingSchema.CreateLeaderboard("chosen_milestone_best_time"));
        _bindingEngine.RegisterSchema(BindingSchema.CreateLeaderboard("chosen_milestone_average"));
        _bindingEngine.RegisterSchema(BindingSchema.CreateLeaderboard("chosen_milestone_amount"));
        //TODO: 0 Uwzglednic wiecej danych z leaderboard'a
        //wiecej od leaderboard bedzie...
    }
    
    private void InitializeSubSchemas()
    {
        //Whitelist
        _bindingEngine.RegisterSubSchema(BindingSubSchema.CreateWhitelist("head"));
        _bindingEngine.RegisterSubSchema(BindingSubSchema.CreateWhitelist("display_name"));
        _bindingEngine.RegisterSubSchema(BindingSubSchema.CreateWhitelist("ign"));
        _bindingEngine.RegisterSubSchema(BindingSubSchema.CreateWhitelist("pb"));
        _bindingEngine.RegisterSubSchema(BindingSubSchema.CreateWhitelist("team_name"));
        _bindingEngine.RegisterSubSchema(BindingSubSchema.CreateWhitelist("stream_name"));
        _bindingEngine.RegisterSubSchema(BindingSubSchema.CreateWhitelist("stream_type"));
    }
    
    private void InitializeConnections()
    {
        _bindingEngine.RegisterConnections(BindingSchema.GetPOV(), BindingSubSchema.GetWhitelistSubSchema());
        _bindingEngine.RegisterConnections(BindingSchema.GetLeaderboard(), BindingSubSchema.GetWhitelistSubSchema());
    }
    
    private void LoadAppCachedBindings()
    {
        foreach (KeyValuePair<string, SceneItemConfiguration> config in _appCache.SceneItemConfigs)
        {
            _bindingEngine.GetOrCreateNode(config.Value.BindingKey);
        }
    }
}
