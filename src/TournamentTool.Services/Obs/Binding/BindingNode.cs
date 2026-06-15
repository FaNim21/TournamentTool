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

    // private Func<string>? _getter;
    // private IBindingObtainer? _dataSource;

    private string _lastValue = string.Empty;

    
    /// <summary>
    /// Nowy pomysl dla binding node:
    /// - zeby w momencie publish na binding node, sciaga on aktualne dane na bazie bindingkey
    /// - nastepnie sprawdza czy wartosc sie zmienila wzgledem poprzedniej podanej wartosci do targetow
    /// - jezeli wartosci sa rozne, czyli LastValue != Value to wtedy robimy apply value dla targetow
    /// </summary>
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

    public void Publish(string? value = null)
    {
        value ??= _dataGetter.GetData(_key);
        _lastValue = value;
        
        StringBuilder sb = new();

        //TODO: 0 Trzeba zdecydować czy target będzie trzymał publikowaną wartość czy nie, żeby nie musieć trzymać aktualizowanej wartości
        //      w node'zie tylko móc wtedy zależnie od itemu robić aktualizacje
        
        foreach (var target in _targets)
        {
            if (target.LastAppliedValue.Equals(value)) continue;

            string lastValue = target.LastAppliedValue;
            target.ApplyBindingValue(value);
            sb.AppendLine($"Source: {target.SourceName} published from {lastValue} to {value}");
        }
        
        _logger.Debug($"Published binding: {_key} for: {sb}");
    }
    
    //TODO: 0 Czyli pomysl to sciaganie BindingNode w miejscu aktualizacji itemu
    // Moze tez byc sytuacja ze w sumie najbardziej sie bedzie oplacac dac dany item przez publish xd zamiast to gu zapisywac xddd, czyli wtedy BindingObtainer???
    // JEST PROBLEM:
    // - POV publikuje zmiany, ale publikuje wszystkie mozliwe opcje, ktore ma zeby inne itemy mogly przechwycic co potrzebuja
    // - Natomiast zmienilem to na publish od strony node'a co nie ma sensu, bo tylko pov publikuje zmiany, a jedyny node jaki ma to to ze jest povem,
    //   czyli binding empty, bo pov ma tylko zmieniony input kind
    /*public void Publish()
    {
        if (_getter is null) return;

        string value = _getter.Invoke();
        if (string.IsNullOrEmpty(value) || value.Equals(LastValue)) return;

        LastValue = value;

        foreach (var target in _targets)
        {
            target.ApplyBindingValue(value);
        }
    }*/
}