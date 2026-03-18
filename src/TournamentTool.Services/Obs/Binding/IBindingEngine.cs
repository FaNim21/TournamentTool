using TournamentTool.Domain.Obs;

namespace TournamentTool.Services.Obs.Binding;

public interface IBindingEngine
{
    IReadOnlyCollection<BindingSchema> AvailableSchemas { get; }

    void UpsertItem(string uuid, BindingKey key);
    
    void RegisterTarget(BindingKey key, IBindingTarget target);
    Task PublishAsync(BindingKey key, object? value);
    
    void RegisterSchema(BindingSchema schema);
}