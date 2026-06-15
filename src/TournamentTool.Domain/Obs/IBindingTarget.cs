namespace TournamentTool.Domain.Obs;

public interface IBindingTarget
{
    string LastAppliedValue { get; }
    string SourceName { get; }
    
    void ApplyBindingValue(string value);
}