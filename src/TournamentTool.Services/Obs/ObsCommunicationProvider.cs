using System.Linq.Expressions;
using System.Reflection;

namespace TournamentTool.Services.Obs;

public interface IObsCommunicationProvider
{
    
}

/// <summary>
/// Klasa odpowiadajaca za podlaczanie rzeczy z leaderboarda itp itd do obs w celu aktualizowania danych z tych paneli w itemach na scenie obsa
/// trzeba tu przekminic sposob 
/// </summary>
public class ObsCommunicationProvider : IObsCommunicationProvider
{
    // private readonly Dictionary<string, List<IBoundItem>> _bindings = new();

    public ObsCommunicationProvider()
    {
        
    }

    /*public void Register(string key, IBoundItem item)
    {
        if (!_bindings.TryGetValue(key, out List<IBoundItem>? value))
        {
            value = [];
            _bindings[key] = value;
        }

        value.Add(item);
    }

    public async Task Publish(string key, object value)
    {
        if (!_bindings.TryGetValue(key, out var items))
            return;

        foreach (var item in items)
            item.Update(value);
    }*/
}