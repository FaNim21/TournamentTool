using TournamentTool.Core.Common.OBS;
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

    void Publish(BindingKey key, object? value);

    bool BindingExists(BindingKey key);
    
    void RegisterSchema(BindingSchema schema);
    void RegisterSubSchema(BindingSubSchema subSchema);
    void RegisterConnections(BindingSchema schema, BindingSubSchema subSchema);
}