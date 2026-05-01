using System.Collections.ObjectModel;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Obs.Binding;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Obs;

public class SceneItemEditWindowViewModel : BaseWindowViewModel
{
    public SceneItemViewModel SceneItemViewModel { get; }

    private InputKind _inputKind = InputKind.unsupported;
    public InputKind InputKind
    {
        get => _inputKind;
        set
        {
            if (value == _inputKind) return;
            _inputKind = value;
            OnPropertyChanged();
        }
    }

    private readonly IReadOnlyCollection<BindingSchema> Schemas;

    public ObservableCollection<string> Sources { get; init; }

    private ObservableCollection<string> _fields = [];
    public ObservableCollection<string> Fields
    {
        get => _fields;
        private set => SetField(ref _fields, value);
    }

    private string _chosenSource = string.Empty;
    public string ChosenSource
    {
        get => _chosenSource;
        set
        {
            _chosenSource = value;
            OnPropertyChanged();

            Fields = new ObservableCollection<string>(Schemas
                .Where(f => f.Source.Equals(ChosenSource))
                .Select(f => f.Field));
        }
    }

    private string _chosenField = string.Empty;
    public string ChosenField
    {
        get => _chosenField;
        set
        {
            _chosenField = value;
            OnPropertyChanged(nameof(ChosenField));
            
            LoadSchema(ChosenField);
        }
    }
    
    private string _sourceName = string.Empty;
    public string SourceName
    {
        get => _sourceName;
        set
        {
            _sourceName = value;
            OnPropertyChanged();
        }
    }

    private int _index;
    public int Index
    {
        get => _index;
        set
        {
            _index = value;
            OnPropertyChanged();
        }
    }

    private bool _isUsingName;
    public bool IsUsingName
    {
        get => _isUsingName;
        set
        {
            _isUsingName = value;
            OnPropertyChanged();
        }
    }

    private bool _isUsingIndex;
    public bool IsUsingIndex
    {
        get => _isUsingIndex;
        set
        {
            _isUsingIndex = value;
            OnPropertyChanged();
        }
    }

    public BindingKey BindingKey { get; private set; } = BindingKey.Empty();
    
    //TODO: 0 Pomysl jest taki zeby name bylo comboboxem, bo mozna sie uprzec i raz na odpalenie edycji pobrac wszystkie nazwy niby

    public SceneItemEditWindowViewModel(SceneItemViewModel sceneItemViewModel, IBindingEngine bindingEngine, AppCache appCache, 
        IDispatcherService dispatcher) : base(dispatcher)
    {
        SceneItemViewModel = sceneItemViewModel;
        InputKind = SceneItemViewModel.InputKind;

        Schemas = bindingEngine.AvailableSchemas;
        Sources = new ObservableCollection<string>(Schemas.Select(s => s.Source).Distinct());
        
        appCache.SceneItemConfigs.TryGetValue(SceneItemViewModel.SourceUUID, out SceneItemConfiguration? config);
        Initialize(config);
    }
    public override void Dispose()
    {
        string? sourceName = string.IsNullOrEmpty(SourceName) ? null : SourceName;
        int? index = Index <= 0 ? null : Index;
        
        BindingKey = new BindingKey(ChosenSource, ChosenField, sourceName, index);
    }

    private void Initialize(SceneItemConfiguration? config)
    {
        BindingKey? binding = config?.BindingKey;
        if (binding == null || binding.IsEmpty()) return;

        ChosenSource = binding.Source;
        ChosenField = binding.Field;
        
        Index = binding.Index ?? 0;
        SourceName = binding.Name ?? string.Empty;
    }

    private void LoadSchema(string field)
    {
        BindingSchema? schema = Schemas.FirstOrDefault(s => 
            s.Source.Equals(ChosenSource, StringComparison.OrdinalIgnoreCase) && 
            s.Field.Equals(field, StringComparison.OrdinalIgnoreCase));
        if (schema == null) return;

        IsUsingName = schema.haveName;
        IsUsingIndex = schema.haveIndex;
    }
}