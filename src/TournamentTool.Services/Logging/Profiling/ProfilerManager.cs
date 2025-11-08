using System.Reflection;

namespace TournamentTool.Domain.Attributes;


public static class ProfilerManager
{
    public static bool IsEnabled { get; set; }
    public static event Action<MethodBase, TimeSpan>? OnProfiled;

    public static void Report(MethodBase method, TimeSpan time)
    {
        OnProfiled?.Invoke(method, time);
    }
}