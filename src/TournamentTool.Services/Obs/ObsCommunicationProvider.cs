using System.Linq.Expressions;
using System.Reflection;

namespace TournamentTool.Services.Obs;

public sealed class ObsSourceBinding
{
    public string SourceName { get; set; }
    public List<>
}

public sealed class ObsPropertyBinding
{
    public string ObsField { get; set; }
    public PropertyInfo CommunicationProperty { get; set; }
}

public interface IObsCommunicationSettings
{
    
}

public interface IObsCommunicationProvider
{
    
}

/// <summary>
/// Klasa odpowiadajaca za podlaczanie rzeczy z leaderboarda itp itd do obs w celu aktualizowania danych z tych paneli w itemach na scenie obsa
/// trzeba tu przekminic sposob 
/// </summary>
public class ObsCommunicationProvider : IObsCommunicationProvider
{

    public ObsCommunicationProvider()
    {
        
    }
}