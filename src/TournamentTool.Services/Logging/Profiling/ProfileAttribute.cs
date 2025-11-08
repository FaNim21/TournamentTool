using System.Diagnostics;
using MethodBoundaryAspect.Fody.Attributes;

namespace TournamentTool.Services.Logging.Profiling;

[Serializable]
[AttributeUsage(AttributeTargets.All)]
public class ProfileAttribute : OnMethodBoundaryAspect
{
    [NonSerialized]
    private Stopwatch? stopwatch;

    public override void OnEntry(MethodExecutionArgs args)
    {
        if (!ProfilerManager.IsEnabled) return;

        stopwatch = Stopwatch.StartNew();
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        if (!ProfilerManager.IsEnabled || stopwatch == null) return;

        stopwatch.Stop();
        ProfilerManager.Report(args.Method, stopwatch.Elapsed);
    }
}