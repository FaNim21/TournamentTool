using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Factories;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Obs.Binding;

public sealed class BindingEngine : IBindingEngine, IDisposable
{
    private readonly ILoggingService _logger;
    private readonly IBindingNodeFactory _bindingNodeFactory;
    private readonly ITournamentState _tournamentState;

    private readonly HashSet<BindingSchema> _availableSchemas = [];
    private readonly HashSet<BindingSubSchema> _availableSubSchemas = [];
    private readonly Dictionary<string, HashSet<BindingSubSchema>> _schemaToSubSchemaConnection = [];
    
    private readonly Dictionary<BindingKey, BindingNode> _nodes = [];

    
    public BindingEngine(ILoggingService logger, IBindingNodeFactory bindingNodeFactory, ITournamentState tournamentState)
    {
        _logger = logger;
        _bindingNodeFactory = bindingNodeFactory;
        _tournamentState = tournamentState;

        _tournamentState.PresetChanged += OnPresetChanged;
    }
    public void Dispose()
    {
        _tournamentState.PresetChanged -= OnPresetChanged;
    }

    public IReadOnlyDictionary<BindingKey, BindingNode> Nodes => _nodes;
    
    public IReadOnlyCollection<BindingSchema> AvailableSchemas => _availableSchemas;
    public IReadOnlyCollection<BindingSubSchema> AvailableSubSchemas => _availableSubSchemas;
    public IReadOnlyDictionary<string, HashSet<BindingSubSchema>> SchemaToSubSchemaConnection => _schemaToSubSchemaConnection;

    
    private void OnPresetChanged(object? sender, Tournament? e)
    {
        PublishAll();
    }
    
    public BindingNode? GetOrCreateNode(BindingKey key)
    {
        if (key is null || key.IsEmpty()) return null;
        if (_nodes.TryGetValue(key, out var node)) return node;

        node = _bindingNodeFactory.Create(key);
        _nodes[key] = node;
        return node;
    }

    public void RegisterTarget(BindingKey key, IBindingTarget target)
    {
        if (key.IsEmpty()) return;

        BindingNode? node = GetOrCreateNode(key);
        node?.AddTarget(target);
    }
    public void RemoveTarget(BindingKey key, IBindingTarget target)
    {
        if (!_nodes.TryGetValue(key, out var node)) return;

        node.RemoveTarget(target);
        //TODO: 0 Jest kwestia usuwania node'ow
        //      ale czy jest potrzeba tego robic i tak? skoro jest i tak ich ograniczona ilosc itd
    }

    public void PublishAll<T>() where T : BindingKey
    {
        foreach (KeyValuePair<BindingKey, BindingNode> node in _nodes)
        {
            if (node.Key is not T) continue;
            node.Value.Publish();
        }
    }
    
    public void PublishAll()
    {
        foreach (BindingNode node in _nodes.Values)
        {
            node.Publish();
        }
    }
    
    public void Publish(BindingKey key, string value)
    {
        if (!_nodes.TryGetValue(key, out BindingNode? node)) return;
        
        //TODO: 0 Nowy pomysl - publikowanie po samym argumencie zaleznie od typu klucza, czyli pov = sourceName, leaderboard = position,
        //      a management leci po calosci

        node.Publish(value);
    }
    
    public bool BindingExists(BindingKey key) => _nodes.TryGetValue(key, out _);

    public void RegisterSchema(BindingSchema schema) => _availableSchemas.Add(schema);
    public void RegisterSubSchema(BindingSubSchema subSchema) => _availableSubSchemas.Add(subSchema);
    public void RegisterConnections(BindingSchema schema, BindingSubSchema subSchema)
    {
        string schemaName = schema.Name;
        
        if (_schemaToSubSchemaConnection.TryGetValue(schemaName, out HashSet<BindingSubSchema>? subSchemas))
        {
            subSchemas.Add(subSchema);
            return;
        }

        HashSet<BindingSubSchema> newSubSchemas = [subSchema];
        _schemaToSubSchemaConnection.Add(schemaName, newSubSchemas);
    }
}