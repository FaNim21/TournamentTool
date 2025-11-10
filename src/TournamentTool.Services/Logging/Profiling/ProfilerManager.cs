using System.Reflection;

namespace TournamentTool.Services.Logging.Profiling;


public static class ProfilerManager
{
    public static bool IsEnabled { get; set; }
    public static event Action<string, string, TimeSpan>? OnProfiled;

    public static void Report(string className, string methodName, TimeSpan time)
    {
        OnProfiled?.Invoke(className, methodName, time);
    }
}