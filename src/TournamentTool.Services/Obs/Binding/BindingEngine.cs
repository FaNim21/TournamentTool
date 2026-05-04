using TournamentTool.Domain.Obs;

namespace TournamentTool.Services.Obs.Binding;

public class BindingEngine : IBindingEngine
{
    public IReadOnlyCollection<BindingSchema> AvailableSchemas => _availableSchemas;
    
    private readonly HashSet<BindingSchema> _availableSchemas = [];
    
    private readonly Dictionary<BindingKey, BindingNode> _nodes = [];
    
    public BindingNode? GetOrCreateNode(BindingKey key)
    {
        if (key is null) return null;
        
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
        if (!_nodes.TryGetValue(key, out var node)) return;

        node.Publish(value);
    }
    
    public void RegisterSchema(BindingSchema schema) => _availableSchemas.Add(schema);
}