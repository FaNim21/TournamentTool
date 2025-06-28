namespace TournamentTool.Utils.Extensions;

public static class TimeSpanExtensions
{
    public static string ToFormattedTime(this TimeSpan span, string prefix = "")
    {
        return $"{prefix}{(int)span.TotalMinutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}";
    }
    public static string ToSimpleFormattedTime(this TimeSpan span, string prefix = "")
    {
        return $"{prefix}{(int)span.TotalMinutes:D2}:{span.Seconds:D2}";
    }
}