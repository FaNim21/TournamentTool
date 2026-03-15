namespace TournamentTool.Services.Obs;

public interface IBindingTarget
{
    Task ApplyBindingValueAsync(object? value);
}