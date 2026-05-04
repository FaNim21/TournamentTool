using TournamentTool.Domain.Obs;

namespace TournamentTool.Services.Obs.Binding;

public sealed class BindingNode
{
    public BindingKey Key { get; }

    private readonly List<IBindingTarget> _targets = [];
    private object? _lastValue;

    public BindingNode(BindingKey key)
    {
        Key = key;
    }

    public void AddTarget(IBindingTarget target)
    {
        _targets.Add(target);
        
        if (_lastValue is null) return;
        target.ApplyBindingValue(_lastValue);
    }
    public void RemoveTarget(IBindingTarget target) => _targets.Remove(target);

    public void Publish(object? value)
    {
        _lastValue = value;
        
        foreach (var target in _targets)
        {
            target.ApplyBindingValue(value);
        }
    }
}