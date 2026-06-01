using TournamentTool.Domain.Obs;
using TournamentTool.Services.Logging;

namespace TournamentTool.Services.Obs.Binding;

public sealed class BindingEngine(ILoggingService logger) : IBindingEngine
{
    public IReadOnlyCollection<BindingSchema> AvailableSchemas => _availableSchemas;
    public IReadOnlyCollection<BindingSubSchema> AvailableSubSchemas => _availableSubSchemas;
    public IReadOnlyDictionary<string, HashSet<BindingSubSchema>> SchemaToSubSchemaConnection => _schemaToSubSchemaConnection;

    private readonly HashSet<BindingSchema> _availableSchemas = [];
    private readonly HashSet<BindingSubSchema> _availableSubSchemas = [];
    private readonly Dictionary<string, HashSet<BindingSubSchema>> _schemaToSubSchemaConnection = [];
    
    private readonly Dictionary<BindingKey, BindingNode> _nodes = [];
    
    public BindingNode? GetOrCreateNode(BindingKey key)
    {
        if (key is null || key.IsEmpty()) return null;
        if (_nodes.TryGetValue(key, out var node)) return node;
        
        node = new BindingNode(key);
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
    }

    public void Publish(BindingKey key, object? value)
    {
        if (!_nodes.TryGetValue(key, out var node))
        {
            logger.Debug($"Cannot found binding: {key} - to publish: {value}");
            return;
        }

        logger.Debug($"Published binding: {key} - with value: {value}");
        node.Publish(value);
    }
    
    public bool ExistBinding(BindingKey key) => _nodes.TryGetValue(key, out _);

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