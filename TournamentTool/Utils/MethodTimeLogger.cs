using System.Reflection;

namespace TournamentTool.Utils;

public static class MethodTimeLogger
{
    public static void Log(MethodBase methodBase, TimeSpan timeSpan, string message)
    {
        Console.WriteLine("{0}.{1} {2}", methodBase.DeclaringType!.Name, methodBase.Name, timeSpan);
    }
}