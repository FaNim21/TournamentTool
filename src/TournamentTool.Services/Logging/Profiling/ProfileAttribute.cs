using System.Diagnostics;
using MethodBoundaryAspect.Fody.Attributes;

namespace TournamentTool.Services.Logging.Profiling;

[Serializable]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ProfileAttribute : OnMethodBoundaryAspect
{
    [NonSerialized] private Stopwatch? stopwatch;
    
    
    public override void OnEntry(MethodExecutionArgs args)
    {
        if (!ProfilerManager.IsEnabled || NeedsToBeIgnored(args)) return;
        
        stopwatch = Stopwatch.StartNew();
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        if (!ProfilerManager.IsEnabled || stopwatch == null || NeedsToBeIgnored(args)) return;
        
        stopwatch.Stop();
        ProfilerManager.Report(args.Method, stopwatch.Elapsed);
    }

    private bool NeedsToBeIgnored(MethodExecutionArgs args)
    {
        return args.Method.IsSpecialName ||
               args.Method.IsConstructor ||
               (args.Method.DeclaringType != null &&
                args.Method.DeclaringType.Name.StartsWith('<'));
    }
}