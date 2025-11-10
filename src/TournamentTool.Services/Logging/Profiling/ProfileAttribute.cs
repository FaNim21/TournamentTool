using System.Diagnostics;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace TournamentTool.Services.Logging.Profiling;

public class ProfilingFabric : ProjectFabric
{
    public override void AmendProject(IProjectAmender amender)
    {
        amender
            .SelectMany(p => p.AllTypes)
            .Where(t =>
                t.ContainingNamespace?.FullName.StartsWith("TournamentTool") == true &&
                !t.ContainingNamespace.FullName.StartsWith("TournamentTool.Services.Logging") &&
                !t.Name.Contains("<") &&
                t.TypeKind == TypeKind.Class)
            .AddAspectIfEligible<ProfileAttribute>();
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ProfileAttribute : Attribute, IAspect<IMethod>, IAspect<INamedType>
{
    void IAspect<IMethod>.BuildAspect(IAspectBuilder<IMethod> builder)
    {
        builder.Override(nameof(OverrideMethod));
    }
    void IAspect<INamedType>.BuildAspect(IAspectBuilder<INamedType> builder)
    {
        foreach (var method in builder.Target.Methods)
        {
            if (method.MethodKind == MethodKind.Default && 
                !method.Name.StartsWith(".") &&
                !method.Name.Contains("<"))
            {
                builder.With(method).Override(nameof(OverrideMethod));
            }
        }
    }
    
    [Template]
    dynamic? OverrideMethod()
    {
        if (!ProfilerManager.IsEnabled) return meta.Proceed();
        
        var method = meta.Target.Method;
        var className = method.DeclaringType.Name;
        var methodName = method.Name;
        
        /*string? caller = null;
        try
        {
            var stack = new StackTrace(1, false);
            var frame = stack.GetFrame(1);
            caller = frame?.GetMethod()?.DeclaringType?.FullName + "." + frame?.GetMethod()?.Name;
        }
        catch
        {
            caller = "<unknown>";
        }*/
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = meta.Proceed();
            
            stopwatch.Stop();
            ProfilerManager.Report(className, methodName, stopwatch.Elapsed);
            
            return result;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            throw;
        }
        finally
        {
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }
                
            ProfilerManager.Report(className, methodName, stopwatch.Elapsed);
        }
    }
}