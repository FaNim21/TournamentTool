using TournamentTool.Domain.Obs;

namespace TournamentTool.Services.Obs.Binding;

public sealed class BindingItem
{
    public string ItemUuid { get; }
    public object? CachedValue { get; private set; }

    public BindingKey Key { get; set; }
    public IBindingTarget? Target { get; private set; }


    public BindingItem(string itemUuid, BindingKey key)
    {
        ItemUuid = itemUuid;
        Key = key;
    }

    public void AttachTarget(IBindingTarget target)
    {
        Target = target;

        if (CachedValue == null) return;
        _ = target.ApplyBindingValueAsync(CachedValue);
    }
    public void DetachTarget()
    {
        Target = null;
    }

    public async Task PublishAsync(object? value)
    {
        CachedValue = value;

        if (Target == null) return;
        await Target.ApplyBindingValueAsync(value);
    }
}

public class BindingEngine : IBindingEngine
{
    public IReadOnlyCollection<BindingSchema> AvailableSchemas => _availableSchemas;
    
    private readonly HashSet<BindingSchema> _availableSchemas = [];
    private readonly Dictionary<BindingKey, List<IBindingTarget>> _bindings = [];
    
    private readonly Dictionary<string, BindingItem> _items = [];
    private readonly Dictionary<BindingKey, List<string>> _index = [];
    
    //TODO: 0 Sa 2 problemy:
    //- List<IBindingTarget> nie recyklinguje sie, przez co sa duplikaty przy odswiezaniu sceny
    //- To, ze przy wczytywaniu itemow rejestracja itemow jest za wolna przez to, ze pov robic initialize z czytaniem danych z obsa i laduje juz znajdujacego sie
    //tam gracza, a w tym samym momencie binding engine nie ma zaladowanych wszystkich itemow wiec nie dla wszystkich targetow da pbulish, bo moze ich nie byc jeszcze na scenie
    
    //TUTAJ ROZWIAZANIEM MOZE BYC BindingGraph i zarzadzanie lifecycle samemu przez scene scene itemy i w przyszlosci moze inne rzeczy (fajnie jakby to bylo nie tylko OBS)
    //Jak wyglada lifecycle IBindingTarget: (DOKONCZYC ROZKMINE TEGO)
    //- Zostaje dodany przy czytaniu scene itemu

    public void RegisterItem(string uuid, BindingKey key)
    {
        BindingItem item = new(uuid, key);

        _items[uuid] = item;

        if (!_index.TryGetValue(key, out List<string>? list))
        {
            list = [];
            _index[key] = list;
        }

        list.Add(uuid);
    }

    public void UpsertItem(string uuid, BindingKey key)
    {
        if (!_items.TryGetValue(uuid, out BindingItem? item))
        {
            item = new BindingItem(uuid, key);
            _items[uuid] = item;
        }
        else
        {
            if (!item.Key.Equals(key))
            {
                _index[item.Key].Remove(uuid);
                item.Key = key;
            }
        }

        _index[key].Add(uuid);
    }
    
    public void AttachTarget(string uuid, IBindingTarget target)
    {
        if (!_items.TryGetValue(uuid, out var item)) return;

        item.AttachTarget(target);
    } 
    
    public void DetachTarget(string uuid)
    {
        if (_items.TryGetValue(uuid, out var item))
            item.DetachTarget();
    }
    
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
        if (!_index.TryGetValue(key, out List<string>? uuids)) return;
        if (uuids == null) return;
        
        for (int i = 0; i < uuids.Count; i++)
        {
            BindingItem item = _items[uuids[i]];
            await item.PublishAsync(value);
        }
    } 
    
    public void RegisterSchema(BindingSchema schema) => _availableSchemas.Add(schema);
}