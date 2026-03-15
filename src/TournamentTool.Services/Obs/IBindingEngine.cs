using TournamentTool.Domain.Obs;

namespace TournamentTool.Services.Obs;

public interface IBindingEngine
{
    IReadOnlyCollection<BindingSchema> AvailableSchemas { get; }

    void RegisterTarget(BindingKey key, IBindingTarget target);
    Task PublishAsync(BindingKey key, object? value);
    
    void RegisterSchema(BindingSchema schema);
}