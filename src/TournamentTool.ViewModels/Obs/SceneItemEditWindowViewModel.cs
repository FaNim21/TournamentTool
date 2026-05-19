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
            //TODO: 0 Zostalo zrobic tylko powiazanie tak zeby zdefiniowac na jakim inputkind jakie bindingi moga byc
        }
    }

    private readonly IReadOnlyCollection<BindingSchema> AllSchemas;

    private BindingViewModelBase? _bindingViewModelBase;
    public BindingViewModelBase? BindingConfigurationViewModel
    {
        get => _bindingViewModelBase;
        private set => SetField(ref _bindingViewModelBase, value);
    }

    public ObservableCollection<string> Schemas { get; init; }

    private string _chosenSchema = string.Empty;
    public string ChosenSchema
    {
        get => _chosenSchema;
        set
        {
            if (_chosenSchema.Equals(value)) return;
            
            _chosenSchema = value;
            OnPropertyChanged();

            BindingSchema? configSchema = AllSchemas.FirstOrDefault(schema => schema.Name.Equals(value, StringComparison.OrdinalIgnoreCase));
            LoadBindingViewModel(configSchema);
        }
    }

    private SceneItemConfiguration? _configuration;
    
    
    public SceneItemEditWindowViewModel(SceneItemViewModel sceneItemViewModel, SceneViewModel sceneViewModel, IBindingEngine bindingEngine, AppCache appCache, 
        IDispatcherService dispatcher) : base(dispatcher)
    {
        _sceneViewModel = sceneViewModel;
        
        SceneItemViewModel = sceneItemViewModel;
        InputKind = SceneItemViewModel.InputKind;

        AllSchemas = bindingEngine.AvailableSchemas;
        Schemas = [.. AllSchemas.DistinctBy(s => s.Name).Select(s => s.Name)];

        appCache.SceneItemConfigs.TryGetValue(SceneItemViewModel.SourceUUID, out SceneItemConfiguration? config);
        _configuration = config;

        string configSchemaName = config?.BindingKey.GetSchema()?.Name ?? string.Empty;
        ChosenSchema = Schemas.FirstOrDefault(schema => schema.Equals(configSchemaName)) ?? string.Empty;
    }

    public void LoadBindingViewModel(BindingSchema? schema)
    {
        if (schema is null)
        {
            BindingConfigurationViewModel = null;
            return;
        }
        
        BindingConfigurationViewModel = schema switch
        {
            BindingPOVSchema => new BindingPovViewModel(AllSchemas.OfType<BindingPOVSchema>().ToList(), _sceneViewModel, _configuration?.BindingKey, Dispatcher),
            BindingRankedManagement => new BindingRankedManagementViewModel(Dispatcher),
            _ => null
        };
    }

    public BindingKey GetBindingKey() => BindingConfigurationViewModel?.GetBindingKey() ?? BindingKey.CreateEmpty();
}