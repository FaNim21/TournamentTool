using System.Text;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Logging;

namespace TournamentTool.Services.Obs.Binding;

public sealed class BindingNode
{
    private readonly ILoggingService _logger;
    private readonly IBindingDataGetter _dataGetter;
    
    private readonly List<IBindingTarget> _targets = [];
    private BindingKey _key;

    private string _lastValue = string.Empty;

    
    public BindingNode(BindingKey key, ILoggingService logger, IBindingDataGetter dataGetter)
    {
        _key = key;
        _logger = logger;
        _dataGetter = dataGetter;
    }

    public void AddTarget(IBindingTarget target)
    {
        _targets.Add(target);
        
        if (_lastValue is null) return;
        target.ApplyBindingValue(_lastValue);
    }
    public void RemoveTarget(IBindingTarget target) => _targets.Remove(target);

    /// <summary>
    /// Pomysl na usprawnienie:
    /// - Dać opcje przekazywania specyfikacji jak position w leaderboardzie czy pov name w pov'ie zeby nie aktualizować wszystkich danego bindingu
    /// </summary>
    public void Publish(string? value = null)
    {
        value ??= _dataGetter.GetData(_key);
        _lastValue = value;
        
        StringBuilder sb = new();

        foreach (var target in _targets)
        {
            if (target.LastAppliedValue.Equals(value)) continue;

            string lastValue = target.LastAppliedValue;
            target.ApplyBindingValue(value);
            sb.AppendLine($"Source: {target.SourceName} published from {lastValue} to {value}");
        }
        
        _logger.Debug($"Published binding: {_key} for: {sb}");
    }
}