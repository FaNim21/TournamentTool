namespace TournamentTool.Services.Obs.Binding;

public interface IBindingTarget
{
    Task ApplyBindingValueAsync(object? value);
}