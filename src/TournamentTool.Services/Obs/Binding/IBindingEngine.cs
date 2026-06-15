using TournamentTool.Domain.Obs;

namespace TournamentTool.Services.Obs.Binding;

public interface IBindingEngine
{
    IReadOnlyDictionary<BindingKey, BindingNode> Nodes { get; }
    
    IReadOnlyCollection<BindingSchema> AvailableSchemas { get; }
    IReadOnlyCollection<BindingSubSchema> AvailableSubSchemas { get; }
    IReadOnlyDictionary<string, HashSet<BindingSubSchema>> SchemaToSubSchemaConnection { get; }
    
    BindingNode? GetOrCreateNode(BindingKey key);

    void RegisterTarget(BindingKey key, IBindingTarget target);
    void RemoveTarget(BindingKey key, IBindingTarget target);

    void PublishAll<T>() where T : BindingKey;
    void PublishAll();
    void Publish(BindingKey key, string value);

    bool BindingExists(BindingKey key);
    
    void RegisterSchema(BindingSchema schema);
    void RegisterSubSchema(BindingSubSchema subSchema);
    void RegisterConnections(BindingSchema schema, BindingSubSchema subSchema);
}