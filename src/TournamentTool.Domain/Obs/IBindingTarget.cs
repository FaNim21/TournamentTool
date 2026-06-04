namespace TournamentTool.Domain.Obs;

public interface IBindingTarget
{
    void ApplyBindingValue(object? value);
}