using System.Collections.ObjectModel;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Obs.Binding;
using TournamentTool.ViewModels.Obs.Bindings;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Obs;

public class SceneItemEditWindowViewModel : BaseWindowViewModel
{
    private readonly SceneViewModel _sceneViewModel;
    private readonly IBindingEngine _bindingEngine;
    
    public SceneItemViewModel SceneItemViewModel { get; }

    private ObservableCollection<InputKind> _supportedInputKinds = [];
    public ObservableCollection<InputKind> SupportedInputKinds
    {
        get => _supportedInputKinds;
        set => SetField(ref _supportedInputKinds, value);
    }

    private InputKind _inputKind = InputKind.unsupported;
    public InputKind InputKind
    {
        get => _inputKind;
        set
        {
            if (_inputKind == value) return;

            _inputKind = value;
            OnPropertyChanged();

            SupportedInputKinds = [.. _inputKind.GetSupportedInputKinds()];
            
            ShowSchemaOptions = _inputKind.IsSupportingBinding();
            if (!ShowSchemaOptions)
            {
                ChosenSchema = "Empty";
                return;
            }

            LoadSchemaFromConfig();
        }
    }

    private readonly IReadOnlyCollection<BindingSchema> AllSchemas;

    private BindingViewModelBase? _bindingViewModelBase;
    public BindingViewModelBase? BindingConfigurationViewModel
    {
        get => _bindingViewModelBase;
        private set => SetField(ref _bindingViewModelBase, value);
    }

    public ObservableCollection<string> Schemas { get; init; } = [];

    private string _chosenSchema = "Empty";
    public string ChosenSchema
    {
        get => _chosenSchema;
        set
        {
            _chosenSchema = value;
            OnPropertyChanged();

            if (string.IsNullOrEmpty(_chosenSchema) || _chosenSchema.Equals("Empty"))
            {
                LoadBindingViewModel(null);
                return;
            }
            
            BindingSchema? configSchema = AllSchemas.FirstOrDefault(schema => schema.Name.Equals(_chosenSchema, StringComparison.OrdinalIgnoreCase));
            LoadBindingViewModel(configSchema);
        }
    }
    
    private bool _showSchemaOptions = true;
    public bool ShowSchemaOptions
    {
        get => _showSchemaOptions;
        set => SetField(ref _showSchemaOptions, value);
    }

    private SceneItemConfiguration? _configuration;
    
    
    public SceneItemEditWindowViewModel(SceneItemViewModel sceneItemViewModel, SceneViewModel sceneViewModel, IBindingEngine bindingEngine, AppCache appCache, 
        IDispatcherService dispatcher) : base(dispatcher)
    {
        _sceneViewModel = sceneViewModel;
        _bindingEngine = bindingEngine;

        SceneItemViewModel = sceneItemViewModel;
        InputKind = SceneItemViewModel.InputKind;

        AllSchemas = bindingEngine.AvailableSchemas;
        Schemas = ["Empty", .. AllSchemas.DistinctBy(s => s.Name).Select(s => s.Name)];

        appCache.SceneItemConfigs.TryGetValue(SceneItemViewModel.SourceUUID, out SceneItemConfiguration? config);
        _configuration = config;
        
        LoadSchemaFromConfig();
    }

    public void LoadBindingViewModel(BindingSchema? schema)
    {
        if (schema is null)
        {
            BindingConfigurationViewModel = null;
            return;
        }
        
        string schemaName = schema.Name;
        ObservableCollection<string> fields = [.. AllSchemas.Where(s => s.Name.Equals(schemaName) && !string.IsNullOrEmpty(s.Field)).Select(s => s.Field)];;
        
        if (_bindingEngine.SchemaToSubSchemaConnection.TryGetValue(schemaName, out var subSchemas))
        {
            foreach (BindingSubSchema subSchema in subSchemas)
            {
                string subSchemaName = subSchema.Name;
                foreach (BindingSubSchema availableSubSchema in _bindingEngine.AvailableSubSchemas)
                {
                    if (!subSchemaName.Equals(availableSubSchema.Name)) continue;
                    
                    fields.Add(availableSubSchema.Field);
                }
            }
        }
        
        BindingConfigurationViewModel = schema switch
        {
            BindingPOVSchema => new BindingPovViewModel(fields, _sceneViewModel, _configuration?.BindingKey, Dispatcher),
            BindingRankedManagementSchema => new BindingRankedManagementViewModel(fields, _configuration?.BindingKey, Dispatcher),
            BindingLeaderboardSchema => new BindingLeaderboardViewModel(fields, _configuration?.BindingKey, Dispatcher),
            _ => null
        };
    }
    
    private void LoadSchemaFromConfig()
    {
        string configSchemaName = _configuration?.BindingKey.GetSchema()?.Name ?? "Empty";
        ChosenSchema = Schemas.FirstOrDefault(schema => schema.Equals(configSchemaName)) ?? string.Empty;
    }

    public BindingKey GetBindingKey() => BindingConfigurationViewModel?.GetBindingKey() ?? BindingKey.CreateEmpty();
}