using TournamentTool.Domain.Obs;

namespace TournamentTool.Services.Obs;

public class BindingEngine : IBindingEngine
{
    public IReadOnlyCollection<BindingSchema> AvailableSchemas => _availableSchemas;
    
    private readonly HashSet<BindingSchema> _availableSchemas = [];
    private readonly Dictionary<BindingKey, List<IBindingTarget>> _bindings = [];
    

    public void RegisterTarget(BindingKey key, IBindingTarget target)
    {
        if (key.IsEmpty()) return;
        
        if (!_bindings.TryGetValue(key, out var list))
        {
            list = [];
            _bindings[key] = list;
        }

        list.Add(target);
    }

    public async Task PublishAsync(BindingKey key, object? value)
    {
        if (!_bindings.TryGetValue(key, out var list)) return;

        foreach (var target in list)
        {
            await target.ApplyBindingValueAsync(value);
        }
    }
    
    public void RegisterSchema(BindingSchema schema) => _availableSchemas.Add(schema);
}