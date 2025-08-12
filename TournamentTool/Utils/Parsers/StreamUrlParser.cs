using System.Globalization;
using System.Web;
using TournamentTool.Enums;

namespace TournamentTool.Utils.Parsers;

public class StreamUrlParser
{
    public static (string? name, int volumePercent, StreamType twitch) Parse(string url)
    {
        var uri = new Uri(url);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);

        if (uri.Host.Contains("kick.com"))
        {
            var name = uri.AbsolutePath.Trim('/');
            bool isMuted = bool.TryParse(queryParams["muted"], out var mutedVal) && mutedVal;
            int volume = isMuted ? 0 : 100;
            return (name, volume, StreamType.kick);
        }

        if (uri.Host.Contains("youtube.com"))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            var name = segments.Length > 1 ? segments[1] : segments[0];
            bool isMuted = queryParams["mute"] == "1";
            int volume = isMuted ? 0 : 100;
            return (name, volume, StreamType.youtube);
        }

        if (uri.Host.Contains("twitch.tv") && queryParams["channel"] != null)
        {
            var name = queryParams["channel"];
            string volumeStr = queryParams["volume"] ?? "0";

            float volumeValue = 0f;
            if (!string.IsNullOrEmpty(volumeStr))
            {
                float.TryParse(volumeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out volumeValue);
            }

            int volumePercent = (int)Math.Round(volumeValue * 100, MidpointRounding.AwayFromZero);return (name, volumePercent, StreamType.twitch);
        }

        throw new NotSupportedException($"Unknown stream type: {uri.Host}");
    }}