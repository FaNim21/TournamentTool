using System.Collections;
using System.Reflection;

namespace TournamentTool.Services.Obs;

public class OverlayDataContext
{
    
}

public interface IBindingTarget
{
    Task ApplyBindingValueAsync(object? value);
}

public interface IBindingEngine
{
    void Register(string path, IBindingTarget target);
    Task NotifyChangedAsync(string path);
}

public class BindingEngine : IBindingEngine
{
    private readonly OverlayDataContext _context = new();

    private readonly Dictionary<string, List<IBindingTarget>> _bindings = new();

    
    public BindingEngine()
    {
        
    }

    public void Register(string path, IBindingTarget target)
    {
        if (!_bindings.TryGetValue(path, out var list))
        {
            list = [];
            _bindings[path] = list;
        }

        list.Add(target);
    }

    public async Task NotifyChangedAsync(string path)
    {
        foreach (var binding in _bindings)
        {
            if (!binding.Key.StartsWith(path))
                continue;

            object? value = Resolve(binding.Key);

            foreach (var target in binding.Value)
            {
                await target.ApplyBindingValueAsync(value);
            }
        }
    }   
    
    private object? Resolve(string path)
    {
        object? current = _context;

        foreach (var part in path.Split('.'))
        {
            if (current == null)
                return null;

            if (part.Contains('['))
            {
                string name = part[..part.IndexOf('[')];
                string indexText = part[(part.IndexOf('[') + 1)..part.IndexOf(']')];

                int index = int.Parse(indexText);

                PropertyInfo? prop = current.GetType().GetProperty(name);
                IList? list = prop!.GetValue(current) as IList;

                current = list?[index];
            }
            else
            {
                PropertyInfo? prop = current.GetType().GetProperty(part);
                current = prop?.GetValue(current);
            }
        }

        return current;
    }
}